using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.Extensions.AI;

namespace VibeCast.Infrastructure.AI;

public sealed class ChatResilienceClient(
    IChatClient innerClient,
    TimeSpan timeout,
    RateLimiter rateLimiter) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutSource.CancelAfter(timeout);

        using RateLimitLease lease = await rateLimiter.AcquireAsync(
                permitCount: 1,
                timeoutSource.Token);

        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException( "VibeCast could not acquire capacity " +
                "for the AI request.");
        }

        return await base.GetResponseAsync(
            messages,
            options,
            timeoutSource.Token);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate>
        GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutSource.CancelAfter(timeout);

        using RateLimitLease lease = await rateLimiter.AcquireAsync(
                permitCount: 1,
                timeoutSource.Token);

        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("VibeCast could not acquire capacity " +
                "for the AI request.");
        }

        await foreach ( ChatResponseUpdate update in base.GetStreamingResponseAsync(
                    messages,
                    options,
                    timeoutSource.Token)
                .WithCancellation(timeoutSource.Token))
        {
            yield return update;
        }
    }
}
