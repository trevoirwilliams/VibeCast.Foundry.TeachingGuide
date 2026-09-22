using System.Runtime.CompilerServices;
using System.Text.Json;
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

    [TestMethod]
    public async Task GenerateAsync_RepairsInvalidPlanOnce_AndReturnsValidatedPlan()
    {
        var fakeChatClient = new RepairablePlanChatClient();
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
            Description: "Design resilient AI workflows.",
            TargetAudience: "Enterprise .NET engineers",
            Objective: "Teach safe AI delivery patterns.",
            Tone: "Professional",
            Language: "English (United States)",
            PlannedPublishDate: null);

        EpisodePlanningResult result = await service.GenerateAsync(request);

        Assert.IsTrue(result.RepairAttempted);
        Assert.AreEqual(20, result.Plan.TargetDurationMinutes);
        Assert.AreEqual(3, result.Plan.Segments.Length);
        Assert.AreEqual("policy-v1", result.FormatPolicyVersion);
        Assert.AreEqual(2, fakeChatClient.CallCount);
        Assert.AreEqual(1, fakePolicyProvider.GetCurrentAsyncCallCount);
    }

    [TestMethod]
    public async Task GenerateAsync_Throws_WhenRepairStillViolatesValidation()
    {
        var fakeChatClient = new RepairFailureChatClient();
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
            Description: "Design resilient AI workflows.",
            TargetAudience: "Enterprise .NET engineers",
            Objective: "Teach safe AI delivery patterns.",
            Tone: "Professional",
            Language: "English (United States)",
            PlannedPublishDate: null);

        try
        {
            await service.GenerateAsync(request);
            Assert.Fail("Expected EpisodePlanValidationException when repair still violates validation rules.");
        }
        catch (EpisodePlanValidationException)
        {
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

    private sealed class RepairablePlanChatClient : IChatClient
    {
        public int CallCount { get; private set; }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await InvokeFormatGuidanceToolAsync(options, cancellationToken);

            CallCount++;

            if (CallCount == 1)
            {
                return new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        JsonSerializer.Serialize(
                            new EpisodePlan(
                                Summary: "Invalid draft",
                                TargetDurationMinutes: 20,
                                Segments:
                                [
                                    new(1, "Opening", "Set context.", 7, ["A", "B"]),
                                    new(2, "Core", "Main discussion.", 7, ["C", "D"]),
                                    new(3, "Close", "Wrap up.", 5, ["E", "F"])
                                ],
                                KeyMessages: ["One", "Two"],
                                EvidenceRequirements: ["Need proof"],
                                MediaRequirements: [],
                                EditorialRisks: []))));
            }

            return new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    JsonSerializer.Serialize(
                        new EpisodePlan(
                            Summary: "A practical summary for this episode.",
                            TargetDurationMinutes: 20,
                            Segments:
                            [
                                new(1, "Opening", "Set context.", 7, ["Point A", "Point B"]),
                                new(2, "Core", "Main discussion.", 7, ["Point C", "Point D"]),
                                new(3, "Close", "Wrap up.", 6, ["Point E", "Point F"])
                            ],
                            KeyMessages: ["Message one.", "Message two."],
                            EvidenceRequirements: ["Use source evidence."],
                            MediaRequirements: ["Simplify walkthrough visual."],
                            EditorialRisks: ["Do not overstate confidence."]))));
        }

        private static async Task InvokeFormatGuidanceToolAsync(ChatOptions? options, CancellationToken cancellationToken)
        {
            if (options?.Tools is not IList<AITool> tools || tools.Count == 0)
            {
                return;
            }

            foreach (AITool tool in tools)
            {
                if (tool is not AIFunction function)
                {
                    continue;
                }

                await function.InvokeAsync(new AIFunctionArguments(), cancellationToken);
            }
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

    private sealed class RepairFailureChatClient : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await InvokeFormatGuidanceToolAsync(options, cancellationToken);

            return new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    JsonSerializer.Serialize(
                        new EpisodePlan(
                            Summary: "Still invalid after repair",
                            TargetDurationMinutes: 20,
                            Segments:
                            [
                                new(1, "Opening", "Set context.", 7, ["A", "B"]),
                                new(2, "Core", "Main discussion.", 7, ["C", "D"]),
                                new(3, "Close", "Wrap up.", 5, ["E", "F"])
                            ],
                            KeyMessages: ["One", "Two"],
                            EvidenceRequirements: ["Need proof"],
                            MediaRequirements: [],
                            EditorialRisks: []))));
        }

        private static async Task InvokeFormatGuidanceToolAsync(ChatOptions? options, CancellationToken cancellationToken)
        {
            if (options?.Tools is not IList<AITool> tools || tools.Count == 0)
            {
                return;
            }

            foreach (AITool tool in tools)
            {
                if (tool is not AIFunction function)
                {
                    continue;
                }

                await function.InvokeAsync(new AIFunctionArguments(), cancellationToken);
            }
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
        public int GetCurrentAsyncCallCount { get; private set; }

        public Task<EpisodeFormatGuidance> GetCurrentAsync(
            EpisodeFormatGuidanceContext context,
            CancellationToken cancellationToken = default)
        {
            GetCurrentAsyncCallCount++;

            return Task.FromResult(
                new EpisodeFormatGuidance(
                    PolicyVersion: "policy-v1",
                    TargetDurationMinutes: 20,
                    PacingGuidance: "Steady pace throughout.",
                    Rationale: "Default policy for testing."));
        }
    }
}
