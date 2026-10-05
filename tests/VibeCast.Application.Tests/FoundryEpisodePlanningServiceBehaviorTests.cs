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
    public Task GenerateAsync_WhenInitialPlanIsInvalidAndRepairIsValid_ReturnsRepairedPlan()
    {
        // PRACTICE S08T-01: Use the supplied fake and plan factory to demonstrate a failed initial plan followed by a valid repair. Assert the result, call bound and repair metadata.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S08T-01: implement GenerateAsync_WhenInitialPlanIsInvalidAndRepairIsValid_ReturnsRepairedPlan.");
    }

    [TestMethod]
    public Task GenerateAsync_WhenRepairIsStillInvalid_ThrowsAfterOneRepairAttempt()
    {
        // PRACTICE S08T-02: Arrange two invalid plans. Prove the service rejects the second result without a third model call, and reports the validation failure.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S08T-02: implement GenerateAsync_WhenRepairIsStillInvalid_ThrowsAfterOneRepairAttempt.");
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
