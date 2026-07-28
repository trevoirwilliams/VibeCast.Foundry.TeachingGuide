using Microsoft.Extensions.AI;

namespace VibeCast.Infrastructure.AI;

public static class ChatResponseCompletionGuard
{
    public static void EnsureUsableCompletion(
        ChatFinishReason? finishReason,
        string operationName)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException(
                "An operation name is required.",
                nameof(operationName));
        }

        ChatCompletionOutcome outcome =
            ChatCompletionOutcomeClassifier.Classify(
                finishReason);

        switch (outcome)
        {
            case ChatCompletionOutcome.Completed:
            case ChatCompletionOutcome.Unspecified:
            case ChatCompletionOutcome.ProviderSpecific:
                return;

            case ChatCompletionOutcome.Truncated:
                throw new InvalidOperationException(
                    $"{operationName} ended because the model reached " +
                    "its output limit. Incomplete output cannot be accepted.");

            case ChatCompletionOutcome.ContentFiltered:
                throw new InvalidOperationException(
                    $"{operationName} ended because the response was " +
                    "filtered. The operation will not be retried or repaired.");

            case ChatCompletionOutcome.ToolCallRequested:
                throw new InvalidOperationException(
                    $"{operationName} returned a tool request that was " +
                    "not consumed by the active chat-client pipeline.");

            default:
                throw new InvalidOperationException(
                    $"{operationName} ended with an unsupported " +
                    "completion outcome.");
        }
    }
}
