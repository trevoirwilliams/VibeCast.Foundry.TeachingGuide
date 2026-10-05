using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Application.Validation;
using VibeCast.Infrastructure.AI;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class FoundryEpisodePlanningWithToolServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_Throws_WhenModelSkipsFormatGuidanceTool()
    {
        // Arrange: a chat client that returns immediately without invoking any tool.
        var fakeChatClient = new FakePlanChatClient();
        var fakePolicyProvider = new FakeFormatPolicyProvider();

        var service = new FoundryEpisodePlanningWithToolService(
            fakeChatClient,
            new EpisodePlanValidator(),
            new EpisodeFormatGuidanceValidator(),
            fakePolicyProvider,
            NullLogger<FoundryEpisodePlanningWithToolService>.Instance);

        var request = new GenerateEpisodePlanRequest(
            EpisodeId: Guid.NewGuid(),
            Title: "AI Reliability",
            Description: null,
            TargetAudience: "Enterprise .NET engineers",
            Objective: "Help teams ship AI features safely.",
            Tone: "Professional",
            Language: "English (United States)",
            PlannedPublishDate: null);

        // Act & Assert: the service must throw because the guidance tool was never called.
        try
        {
            await service.GenerateAsync(request);
            Assert.Fail("Expected InvalidOperationException when model skips format guidance tool.");
        }
        catch (InvalidOperationException exception)
        {
            StringAssert.Contains(
                exception.Message,
                "without retrieving current episode-format guidance");
        }
    }

    // Returns a plain ChatResponse without invoking any registered tool.
    private sealed class FakePlanChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new ChatResponse(
                    new ChatMessage(ChatRole.Assistant, string.Empty)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private sealed class FakeFormatPolicyProvider : IEpisodeFormatPolicyProvider
    {
        public Task<EpisodeFormatGuidance> GetCurrentAsync(
            EpisodeFormatGuidanceContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new EpisodeFormatGuidance(
                    PolicyVersion: "policy-v1",
                    TargetDurationMinutes: 20,
                    PacingGuidance: "Steady pace throughout.",
                    Rationale: "Default policy for testing."));
        }
    }
}
