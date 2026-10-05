using System;
using System.Collections.Generic;
using System.Text;
using VibeCast.Application.Episodes;

namespace VibeCast.Application.Media;

public static class EpisodeArtworkPrompt
{
    public const string Version = "episode-artwork-v1";

    public static string Build(EpisodeDetails episode)
    {
        ArgumentNullException.ThrowIfNull(episode);

        EpisodePlan plan =
            episode.AcceptedPlan?.Plan
            ?? throw new InvalidOperationException(
                "An accepted episode plan is required " +
                "before promotional artwork can be generated.");

        string keyMessages = string.Join(
            Environment.NewLine,
            plan.KeyMessages.Select(
                (message, index) =>
                    $"{index + 1}. {message}"));

        return $$"""
            Create one polished landscape promotional illustration
            for this podcast episode.

            <episode>
            Title: {{episode.Title}}
            Audience: {{episode.TargetAudience}}
            Objective: {{episode.Objective}}
            Tone: {{episode.Tone}}
            Summary: {{plan.Summary}}

            Key messages:
            {{keyMessages}}
            </episode>

            The artwork should communicate the central idea visually
            and be suitable for professional podcast promotion.

            Do not include text, captions, logos, watermarks,
            user-interface elements, or podcast-player controls.
            """;
    }
}
