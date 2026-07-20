using System.Threading.Channels;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Jobs;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Infrastructure.Jobs;

public sealed class ChannelBackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<BackgroundJob> _channel;

    public ChannelBackgroundJobQueue(IOptions<BackgroundJobsOptions> options)
    {
        _channel = Channel.CreateBounded<BackgroundJob>(new BoundedChannelOptions(options.Value.Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(BackgroundJob job, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<BackgroundJob> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
