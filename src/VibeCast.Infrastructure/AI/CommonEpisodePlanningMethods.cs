using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;
using VibeCast.Application.Episodes;
using VibeCast.Application.Validation;

namespace VibeCast.Infrastructure.AI;

public abstract class CommonEpisodePlanningMethods
{
    protected static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    protected string CreateEditorialBriefJson(GenerateEpisodePlanRequest request)
    {
        return JsonSerializer.Serialize(
            new
            {
                title = request.Title.Trim(),
                description = request.Description?.Trim(),
                targetAudience =
                    request.TargetAudience.Trim(),
                objective = request.Objective.Trim(),
                tone = request.Tone.Trim(),
                language = request.Language.Trim(),
                plannedPublishDate =
                    request.PlannedPublishDate
            },
            JsonOptions);
    }

    protected ChatMessage[] CreateInitialMessages(string editorialBriefJson)
    {
        return
        [
            new ChatMessage(
                ChatRole.System,
                EpisodePlannerPrompt.Instructions),

            new ChatMessage(
                ChatRole.User,
                $$"""
                  Create one typed episode plan from the following
                  editorial brief.

                  Prompt version: {{EpisodePlannerPrompt.Version}}

                  <editorial-brief>
                  {{editorialBriefJson}}
                  </editorial-brief>
                  """)
        ];
    }

    protected ChatMessage[] CreateRepairMessages(string editorialBriefJson, EpisodePlan invalidPlan, IReadOnlyCollection<ValidationFailure> failures)
    {
        string invalidPlanJson =
            JsonSerializer.Serialize(
                invalidPlan,
                JsonOptions);

        string validationFailuresJson =
            JsonSerializer.Serialize(
                failures.Select(
                    failure => new
                    {
                        propertyName = failure.PropertyName,
                        errorMessage = failure.ErrorMessage
                    }),
                JsonOptions);

        string combinedSystemInstructions =
            $"""
             {EpisodePlannerPrompt.Instructions}

             {EpisodePlannerPrompt.RepairInstructions}
             """;

        return
        [
            new ChatMessage(
                ChatRole.System,
                combinedSystemInstructions),

            new ChatMessage(
                ChatRole.User,
                $$"""
                  Repair the invalid episode plan using the original
                  editorial brief and deterministic validation failures.

                  Repair prompt version:
                  {{EpisodePlannerPrompt.RepairVersion}}

                  <editorial-brief>
                  {{editorialBriefJson}}
                  </editorial-brief>

                  <invalid-episode-plan>
                  {{invalidPlanJson}}
                  </invalid-episode-plan>

                  <validation-failures>
                  {{validationFailuresJson}}
                  </validation-failures>

                  Return one complete replacement episode plan.
                  """)
        ];
    }

}
