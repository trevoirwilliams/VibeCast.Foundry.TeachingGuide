using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Infrastructure.AI;

namespace VibeCast.Application.Tests;

// These checks deliberately fail until the corresponding practice gap is implemented.
// They run against a local fake client: no Azure credentials or paid requests are used.
[TestClass]
public sealed class EpisodeConceptPracticeTests
{
    [TestMethod]
    public async Task GenerateAsync_ReturnsTrimmedTextInApplicationResult()
    {
        using var client = new FakeChatClient("  A useful concept  ");
        using var cancellation = new CancellationTokenSource();
        var generator = CreateGenerator(client);

        EpisodeConceptResult result =
            await generator.GenerateAsync(CreateRequest(), cancellation.Token);

        Assert.AreEqual("A useful concept", result.Content);
        Assert.AreEqual(cancellation.Token, client.LastToken);
    }

    [TestMethod]
    public async Task GenerateAsync_RejectsWhitespaceOutput()
    {
        using var client = new FakeChatClient(" \t\r\n ");
        var generator = CreateGenerator(client);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => generator.GenerateAsync(CreateRequest()));
    }

    [TestMethod]
    public async Task GenerateAsync_PropagatesCallerCancellation()
    {
        using var client = new FakeChatClient("Should not be returned");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var generator = CreateGenerator(client);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => generator.GenerateAsync(CreateRequest(), cancellation.Token));
        Assert.AreEqual(cancellation.Token, client.LastToken);
    }

    [TestMethod]
    public async Task GenerateAsync_RejectsNullRequest()
    {
        using var client = new FakeChatClient("Unused");
        var generator = CreateGenerator(client);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => generator.GenerateAsync(null!));
    }

    [TestMethod]
    public async Task StreamAsync_PreservesOrderAndSpacesWhileSkippingEmptyUpdates()
    {
        using var client = new FakeChatClient("", "Hello", "", " ", "world");
        var generator = CreateGenerator(client);
        List<string> fragments = [];

        await foreach (string fragment in generator.StreamAsync(CreateRequest()))
        {
            fragments.Add(fragment);
        }

        CollectionAssert.AreEqual(
            new[] { "Hello", " ", "world" }, fragments.ToArray());
        Assert.AreEqual("Hello world", string.Concat(fragments));
    }

    [TestMethod]
    public async Task StreamAsync_RejectsAnEmptyStream()
    {
        using var client = new FakeChatClient("");
        var generator = CreateGenerator(client);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await foreach (string _ in generator.StreamAsync(CreateRequest()))
            {
            }
        });
    }

    [TestMethod]
    public async Task StreamAsync_RejectsOnlyEmptyUpdates()
    {
        using var client = new FakeChatClient("", "", "");
        var generator = CreateGenerator(client);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await foreach (string _ in generator.StreamAsync(CreateRequest()))
            {
            }
        });
    }

    [TestMethod]
    public async Task StreamAsync_PropagatesCancellationBetweenUpdates()
    {
        using var client = new FakeChatClient("", "first", "second");
        using var cancellation = new CancellationTokenSource();
        var generator = CreateGenerator(client);
        await using var enumerator = generator
            .StreamAsync(CreateRequest(), cancellation.Token)
            .GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual("first", enumerator.Current);
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await enumerator.MoveNextAsync();
        });
    }

    private static FoundryEpisodeConceptGenerator CreateGenerator(IChatClient client) =>
        new(client, NullLogger<FoundryEpisodeConceptGenerator>.Instance);

    private static GenerateEpisodeConceptRequest CreateRequest() =>
        new(
            Title: "Reliable AI",
            Description: "A practical introduction",
            TargetAudience: ".NET developers",
            Objective: "Understand model integration",
            Tone: "Professional",
            Language: "English");

    private sealed class FakeChatClient(
        string responseText,
        params string[] updates) : IChatClient
    {
        public CancellationToken LastToken { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string text in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
