using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Infrastructure.AI;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class FoundryEpisodePlanningServiceBehaviorTests
{
    [TestMethod]
    public async Task GenerateAsync_WhenInitialPlanIsInvalidAndRepairIsValid_ReturnsRepairedPlan()
    {
        EpisodePlan invalidPlan = CreatePlan(
            summary: "Initial plan with invalid timing.",
            segmentDurations: [5, 5, 5]);

        EpisodePlan repairedPlan = CreatePlan(
            summary: "Repaired plan with valid timing.",
            segmentDurations: [7, 7, 6]);

        var chatClient = new SequenceChatClient(
            invalidPlan,
            repairedPlan);

        var service = new FoundryEpisodePlanningService(
            chatClient,
            new EpisodePlanValidator(),
            NullLogger<FoundryEpisodePlanningService>.Instance);

        EpisodePlanningResult result =
            await service.GenerateAsync(CreateRequest());

        Assert.AreEqual(2, chatClient.CallCount);
        Assert.IsTrue(result.RepairAttempted);
        Assert.AreEqual(
            EpisodePlannerPrompt.RepairVersion,
            result.RepairPromptVersion);
        Assert.AreEqual(
            repairedPlan.Summary,
            result.Plan.Summary);

        Assert.IsTrue(
            new EpisodePlanValidator()
                .Validate(result.Plan)
                .IsValid);
    }

    [TestMethod]
    public async Task GenerateAsync_WhenRepairIsStillInvalid_ThrowsAfterOneRepairAttempt()
    {
        EpisodePlan firstInvalidPlan = CreatePlan(
            summary: "Initial invalid plan.",
            segmentDurations: [5, 5, 5]);

        EpisodePlan secondInvalidPlan = CreatePlan(
            summary: "Repair is still invalid.",
            segmentDurations: [6, 6, 6]);

        var chatClient = new SequenceChatClient(
            firstInvalidPlan,
            secondInvalidPlan);

        var service = new FoundryEpisodePlanningService(
            chatClient,
            new EpisodePlanValidator(),
            NullLogger<FoundryEpisodePlanningService>.Instance);

        EpisodePlanValidationException exception =
            await Assert.ThrowsExactlyAsync<EpisodePlanValidationException>(
                () => service.GenerateAsync(CreateRequest()));

        Assert.AreEqual(2, chatClient.CallCount);
        Assert.IsTrue(exception.RepairAttempted);
        Assert.IsTrue(exception.Failures.Count > 0);
        Assert.IsTrue(
            exception.Failures.Any(
                failure =>
                    failure.PropertyName ==
                    "Segments.TotalDuration"));
    }

    private static GenerateEpisodePlanRequest CreateRequest() =>
        new(
            EpisodeId: Guid.NewGuid(),
            Title: "Reliable AI Systems",
            Description: "How to make AI-backed application behavior predictable.",
            TargetAudience: "Enterprise .NET developers",
            Objective: "Build reliable AI application boundaries.",
            Tone: "Professional",
            Language: "English (United States)",
            PlannedPublishDate: null);

    private static EpisodePlan CreatePlan(
        string summary,
        int[] segmentDurations) =>
        new(
            Summary: summary,
            TargetDurationMinutes: 20,
            Segments:
            [
                new(
                    Sequence: 1,
                    Title: "Opening",
                    Purpose: "Establish the problem.",
                    DurationMinutes: segmentDurations[0],
                    TalkingPoints:
                    [
                        "Why reliability matters.",
                        "Where variability enters."
                    ]),
                new(
                    Sequence: 2,
                    Title: "Core",
                    Purpose: "Explain the engineering approach.",
                    DurationMinutes: segmentDurations[1],
                    TalkingPoints:
                    [
                        "Validate generated state.",
                        "Bound repair attempts."
                    ]),
                new(
                    Sequence: 3,
                    Title: "Close",
                    Purpose: "Summarize the application guarantees.",
                    DurationMinutes: segmentDurations[2],
                    TalkingPoints:
                    [
                        "Protect downstream behavior.",
                        "Retain deterministic evidence."
                    ])
            ],
            KeyMessages:
            [
                "AI output is a candidate until validated.",
                "Repair must remain bounded."
            ],
            EvidenceRequirements: [],
            MediaRequirements: [],
            EditorialRisks: []);

    private sealed class SequenceChatClient : IChatClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly Queue<string> responses;

        public SequenceChatClient(params EpisodePlan[] plans)
        {
            responses = new Queue<string>(
                plans.Select(
                    plan =>
                        JsonSerializer.Serialize(
                            plan,
                            JsonOptions)));
        }

        public int CallCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (responses.Count == 0)
            {
                throw new InvalidOperationException(
                    "The test did not configure another model response.");
            }

            ChatResponse response = new(
                new ChatMessage(
                    ChatRole.Assistant,
                    responses.Dequeue()))
            {
                FinishReason = ChatFinishReason.Stop
            };

            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate>
            GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions? options = null,
                [EnumeratorCancellation]
                CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(
            Type serviceType,
            object? serviceKey = null) =>
            null;

        public void Dispose()
        {
        }
    }
}
