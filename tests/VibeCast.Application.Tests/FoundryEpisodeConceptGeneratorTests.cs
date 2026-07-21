using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Infrastructure.AI;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class FoundryEpisodeConceptGeneratorTests
{
    [TestMethod]
    public async Task GenerateAsync_ReturnsMappedContent_AndBuildsExpectedPrompt()
    {
        const string responseText = "Working title: Reliable AI systems";

        var fakeChatClient = new FakeChatClient
        {
            ResponseText = responseText
        };

        var generator = new FoundryEpisodeConceptGenerator(
            fakeChatClient,
            NullLogger<FoundryEpisodeConceptGenerator>.Instance);

        var request = new GenerateEpisodeConceptRequest(
            Title: "AI Reliability",
            Description: "Designing stable AI-backed product behavior.",
            TargetAudience: "Enterprise .NET engineers",
            Objective: "Help teams ship AI features safely.",
            Tone: "Professional and direct",
            Language: "English (United States)");

        EpisodeConceptResult result = await generator.GenerateAsync(request);

        Assert.AreEqual(responseText, result.Content);
        Assert.AreEqual(1, fakeChatClient.GetResponseAsyncCallCount);
        Assert.IsNotNull(fakeChatClient.LastMessages);
        Assert.IsNotNull(fakeChatClient.LastOptions);

        ChatMessage[] messages = fakeChatClient.LastMessages!;

        Assert.AreEqual(2, messages.Length);
        Assert.AreEqual(ChatRole.System.Value, messages[0].Role.Value);
        Assert.AreEqual(ChatRole.User.Value, messages[1].Role.Value);

        string systemMessage = messages[0].Text;
        string userMessage = messages[1].Text;

        StringAssert.Contains(systemMessage, "You are the editorial concept assistant for VibeCast.");
        StringAssert.Contains(userMessage, "Generate one episode concept from this editorial brief.");
        StringAssert.Contains(userMessage, "\"title\":\"AI Reliability\"");
        StringAssert.Contains(userMessage, "\"description\":\"Designing stable AI-backed product behavior.\"");
        StringAssert.Contains(userMessage, "\"targetAudience\":\"Enterprise .NET engineers\"");
        StringAssert.Contains(userMessage, "\"objective\":\"Help teams ship AI features safely.\"");
        StringAssert.Contains(userMessage, "\"tone\":\"Professional and direct\"");
        StringAssert.Contains(userMessage, "\"language\":\"English (United States)\"");

        Assert.AreEqual(2000, fakeChatClient.LastOptions!.MaxOutputTokens);
    }

    [TestMethod]
    public async Task GenerateAsync_Throws_WhenModelReturnsEmptyText()
    {
        var fakeChatClient = new FakeChatClient
        {
            ResponseText = "   "
        };

        var generator = new FoundryEpisodeConceptGenerator(
            fakeChatClient,
            NullLogger<FoundryEpisodeConceptGenerator>.Instance);

        var request = new GenerateEpisodeConceptRequest(
            Title: "AI Reliability",
            Description: null,
            TargetAudience: "Engineers",
            Objective: "Teach resilient design",
            Tone: "Professional and direct",
            Language: "English (United States)");

        try
        {
            await generator.GenerateAsync(request);
            Assert.Fail("Expected InvalidOperationException for empty model response.");
        }
        catch (InvalidOperationException exception)
        {
            StringAssert.Contains(
                exception.Message,
                "returned an empty episode concept");
        }
    }

    [TestMethod]
    public async Task GenerateAsync_ForwardsCancellationToken_ToChatClient()
    {
        var fakeChatClient = new FakeChatClient
        {
            ResponseText = "Working title: Cancellation test"
        };

        var generator = new FoundryEpisodeConceptGenerator(
            fakeChatClient,
            NullLogger<FoundryEpisodeConceptGenerator>.Instance);

        var request = new GenerateEpisodeConceptRequest(
            Title: "AI Reliability",
            Description: null,
            TargetAudience: "Engineers",
            Objective: "Teach resilient design",
            Tone: "Professional and direct",
            Language: "English (United States)");

        using var cancellation = new CancellationTokenSource();

        await generator.GenerateAsync(request, cancellation.Token);

        Assert.AreEqual(cancellation.Token, fakeChatClient.LastCancellationToken);
        Assert.IsTrue(fakeChatClient.LastCancellationToken.CanBeCanceled);
    }

    private sealed class FakeChatClient : IChatClient
    {
        public int GetResponseAsyncCallCount { get; private set; }

        public ChatMessage[]? LastMessages { get; private set; }

        public ChatOptions? LastOptions { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public string ResponseText { get; init; } = string.Empty;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToArray();
            LastOptions = options;
            LastCancellationToken = cancellationToken;
            GetResponseAsyncCallCount++;

            var response = new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    ResponseText));

            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToArray();
            LastOptions = options;
            LastCancellationToken = cancellationToken;

            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
