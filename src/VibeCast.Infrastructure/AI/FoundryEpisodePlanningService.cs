using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Episodes;
using VibeCast.Application.Validation;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodePlanningService(
    IChatClient chatClient,
    IValidator<EpisodePlan> planValidator,
    ILogger<FoundryEpisodePlanningService> logger) : IEpisodePlanningService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<EpisodePlanningResult> GenerateAsync(GenerateEpisodePlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EpisodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid episode identifier is required.",
                nameof(request));
        }

        string editorialBriefJson =
            CreateEditorialBriefJson(request);

        ChatMessage[] initialMessages =
            CreateInitialMessages(editorialBriefJson);

        EpisodePlan initialPlan =
            await RequestTypedPlanAsync(
                initialMessages,
                operationName: "initial generation",
                cancellationToken);

        ValidationResult initialValidation =
            planValidator.Validate(initialPlan);

        if (initialValidation.IsValid)
        {
            return new EpisodePlanningResult(
                Plan: initialPlan,
                PromptVersion: EpisodePlannerPrompt.Version,
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                RepairAttempted: false,
                RepairPromptVersion: null);
        }

        string[] initialFailurePaths =
            initialValidation.Errors
                .Select(failure => failure.PropertyName)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        logger.LogWarning(
            "The initial typed episode plan for episode " +
            "{EpisodeId} failed deterministic validation. " +
            "FailureCount: {FailureCount}; FailurePaths: " +
            "{FailurePaths}. One bounded repair will be attempted.",
            request.EpisodeId,
            initialValidation.Errors.Count,
            initialFailurePaths);

        ChatMessage[] repairMessages =
            CreateRepairMessages(
                editorialBriefJson,
                initialPlan,
                initialValidation.Errors);

        EpisodePlan repairedPlan =
            await RequestTypedPlanAsync(
                repairMessages,
                operationName: "bounded repair",
                cancellationToken);

        ValidationResult repairedValidation =
            planValidator.Validate(repairedPlan);

        if (!repairedValidation.IsValid)
        {
            string[] repairedFailurePaths =
                repairedValidation.Errors
                    .Select(failure => failure.PropertyName)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            logger.LogWarning(
                "The repaired episode plan for episode " +
                "{EpisodeId} remained invalid. " +
                "FailureCount: {FailureCount}; FailurePaths: " +
                "{FailurePaths}. No additional repair will occur.",
                request.EpisodeId,
                repairedValidation.Errors.Count,
                repairedFailurePaths);

            throw new EpisodePlanValidationException(
                repairedValidation.Errors,
                repairAttempted: true);
        }

        return new EpisodePlanningResult(
            Plan: repairedPlan,
            PromptVersion: EpisodePlannerPrompt.Version,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            RepairAttempted: true,
            RepairPromptVersion:
                EpisodePlannerPrompt.RepairVersion);

    }

    private ChatMessage[] CreateRepairMessages(string editorialBriefJson, EpisodePlan invalidPlan, IReadOnlyCollection<ValidationFailure> failures)
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

    private async Task<EpisodePlan> RequestTypedPlanAsync(ChatMessage[] initialMessages, string operationName, CancellationToken cancellationToken)
    {
        ChatOptions options = new()
        {
            MaxOutputTokens = 3_500
        };

        ChatResponse<EpisodePlan> response =
            await chatClient.GetResponseAsync<EpisodePlan>(
                initialMessages,
                JsonOptions,
                options,
                useJsonSchemaResponseFormat: true,
                cancellationToken: cancellationToken);

        ChatResponseCompletionGuard.EnsureUsableCompletion(
            response.FinishReason,
            operationName);

        if (!response.TryGetResult(out EpisodePlan? plan) ||
            plan is null)
        {
            logger.LogWarning(
                "Microsoft Foundry returned an unusable structured " +
                "episode plan during {OperationName}. " +
                "ResponseId: {ResponseId}; FinishReason: " +
                "{FinishReason}.",
                operationName,
                response.ResponseId,
                response.FinishReason);

            throw new InvalidOperationException(
                $"Microsoft Foundry did not return a usable typed " +
                $"episode plan during {operationName}.");
        }

        return plan;
    }

    private ChatMessage[] CreateInitialMessages(string editorialBriefJson)
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

    private string CreateEditorialBriefJson(GenerateEpisodePlanRequest request)
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
}
