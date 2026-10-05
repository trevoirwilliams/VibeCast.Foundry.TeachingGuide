using System.Text.Json;
using VibeCast.Application.Media;

namespace VibeCast.Application.Episodes;

public static class EpisodeResourceRelevancePrompt
{
    public const string Version =
        "episode-resource-relevance-v1";

    public const string SystemMessage =
        """
        You assess whether an analyzed supporting resource materially
        contributes to an existing episode plan.

        Apply these rules:

        - Judge relevance only against the supplied episode context.
        - Topic overlap alone is not enough.
        - Relevant content must materially support a key message,
          segment, objective, or evidence requirement.
        - Do not introduce facts that are not present in the analyzed
          resource.
        - Summary must describe only useful content from the source.
        - RelevantPoints must contain no more than five concise
          source-grounded points.
        - MatchedEvidenceRequirements may only contain evidence
          requirements supplied in the episode context.
        - Copy matched evidence requirements verbatim.
        - If the resource is not materially useful, set IsRelevant
          to false and return empty RelevantPoints and
          MatchedEvidenceRequirements arrays.
        """;

    public static string BuildUserMessage(
        EpisodeDetails episode,
        MediaAssetSummary source,
        string analyzedContent)
    {
        ArgumentNullException.ThrowIfNull(episode);
        ArgumentNullException.ThrowIfNull(source);

        EpisodePlan plan =
            episode.AcceptedPlan?.Plan
            ?? throw new InvalidOperationException(
                "An accepted episode plan is required.");

        string episodeContext =
            JsonSerializer.Serialize(
                new
                {
                    episode.Title,
                    episode.Description,
                    episode.TargetAudience,
                    episode.Objective,
                    episode.Tone,
                    Plan = new
                    {
                        plan.Summary,
                        plan.KeyMessages,
                        plan.EvidenceRequirements,
                        Segments = plan.Segments.Select(
                            segment => new
                            {
                                segment.Sequence,
                                segment.Title,
                                segment.Purpose,
                                segment.TalkingPoints
                            })
                    }
                });

        return
            $"""
            EPISODE CONTEXT
            {episodeContext}

            SOURCE FILE
            {source.OriginalFileName}

            CONTENT UNDERSTANDING OUTPUT
            {analyzedContent}

            Return the typed relevance assessment.
            """;
    }
}
