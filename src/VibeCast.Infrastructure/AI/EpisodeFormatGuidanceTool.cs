namespace VibeCast.Infrastructure.AI;

public static class EpisodeFormatGuidanceTool
{
    public const string Name =
        "get_episode_format_guidance";

    public const string Description =
        "Retrieves the current VibeCast target duration and pacing " +
        "guidance for the persisted episode audience and tone. " +
        "Call this before finalizing an episode plan.";
}
