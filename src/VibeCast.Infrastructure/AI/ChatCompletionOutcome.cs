using Microsoft.Extensions.AI;

namespace VibeCast.Infrastructure.AI;

public enum ChatCompletionOutcome
{
    Completed,
    Truncated,
    ContentFiltered,
    ToolCallRequested,
    Unspecified,
    ProviderSpecific
}

public static class ChatCompletionOutcomeClassifier
{
    public static ChatCompletionOutcome Classify(
        ChatFinishReason? finishReason)
    {
        if (finishReason is null)
        {
            return ChatCompletionOutcome.Unspecified;
        }

        ChatFinishReason value = finishReason.Value;

        if (value.Equals(ChatFinishReason.Stop))
        {
            return ChatCompletionOutcome.Completed;
        }

        if (value.Equals(ChatFinishReason.Length))
        {
            return ChatCompletionOutcome.Truncated;
        }

        if (value.Equals(ChatFinishReason.ContentFilter))
        {
            return ChatCompletionOutcome.ContentFiltered;
        }

        if (value.Equals(ChatFinishReason.ToolCalls))
        {
            return ChatCompletionOutcome.ToolCallRequested;
        }

        return ChatCompletionOutcome.ProviderSpecific;
    }
}
