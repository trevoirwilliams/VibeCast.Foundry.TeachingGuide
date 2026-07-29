using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Episodes;
using VibeCast.Application.Validation;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodePlanningWithToolService(
    IChatClient chatClient,
    IValidator<EpisodePlan> planValidator,
    IValidator<EpisodeFormatGuidanceValidationRequest>
        formatGuidanceValidator,
    IEpisodeFormatPolicyProvider formatPolicyProvider,
    ILogger<FoundryEpisodePlanningWithToolService> logger)
    : CommonEpisodePlanningMethods, IEpisodePlanningService
{
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

        EpisodeFormatGuidanceContext guidanceContext = new(
            TargetAudience: request.TargetAudience.Trim(),
            Tone: request.Tone.Trim(),
            PlanningTimeUtc: DateTimeOffset.UtcNow);

        EpisodeFormatGuidanceState guidanceState = new();

        ChatMessage[] initialMessages =
            CreateInitialMessages(editorialBriefJson);

        EpisodePlan initialPlan =
            await RequestTypedPlanAsync(
                initialMessages,
                operationName: "initial generation with tool calling",
                episodeId: request.EpisodeId,
                guidanceContext,
                guidanceState,
                cancellationToken);

        EpisodeFormatGuidance selectedGuidance =
            GetSelectedGuidance(guidanceState);

        ValidationResult initialValidation =
            ValidateCandidate(
                initialPlan,
                selectedGuidance);

        if (initialValidation.IsValid)
        {
            return new EpisodePlanningResult(
                Plan: initialPlan,
                PromptVersion: EpisodePlannerPrompt.Version,
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                RepairAttempted: false,
                RepairPromptVersion: null,
                FormatPolicyVersion:
                    selectedGuidance.PolicyVersion);
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
                operationName: "bounded repair with tool calling",
                episodeId: request.EpisodeId,
                guidanceContext,
                guidanceState,
                cancellationToken);

        ValidationResult repairedValidation =
            ValidateCandidate(
                repairedPlan,
                selectedGuidance);

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
               EpisodePlannerPrompt.RepairVersion,
           FormatPolicyVersion:
               selectedGuidance.PolicyVersion);
    }

    private ValidationResult ValidateCandidate(EpisodePlan plan, EpisodeFormatGuidance guidance)
    {
        ValidationResult result =
            planValidator.Validate(plan);

        ValidationResult guidanceResult =
            formatGuidanceValidator.Validate(
                new EpisodeFormatGuidanceValidationRequest(
                    plan,
                    guidance));

        foreach (ValidationFailure failure
                 in guidanceResult.Errors)
        {
            result.Add(
                failure.PropertyName,
                failure.ErrorMessage);
        }

        return result;
    }

    private async Task<EpisodePlan> RequestTypedPlanAsync(ChatMessage[] initialMessages, string operationName, Guid episodeId, EpisodeFormatGuidanceContext guidanceContext, EpisodeFormatGuidanceState guidanceState, CancellationToken cancellationToken)
    {
        int invocationCountBeforeRequest =
            guidanceState.InvocationCount;

        async Task<EpisodeFormatGuidance> GetEpisodeFormatGuidanceAsync(
                CancellationToken toolCancellationToken)
        {
            guidanceState.InvocationCount++;

            if (guidanceState.Guidance is not null)
            {
                return guidanceState.Guidance;
            }

            EpisodeFormatGuidance guidance =
                await formatPolicyProvider.GetCurrentAsync(
                    guidanceContext,
                    toolCancellationToken);

            guidanceState.Guidance = guidance;

            logger.LogInformation(
                "Resolved episode format policy " +
                "{FormatPolicyVersion} for episode {EpisodeId}. " +
                "TargetDurationMinutes: {TargetDurationMinutes}.",
                guidance.PolicyVersion,
                episodeId,
                guidance.TargetDurationMinutes);

            return guidance;
        }

        AIFunction formatGuidanceTool =
            AIFunctionFactory.Create(
                (Func<
                    CancellationToken,
                    Task<EpisodeFormatGuidance>>)
                GetEpisodeFormatGuidanceAsync,
                name: EpisodeFormatGuidanceTool.Name,
                description:
                    EpisodeFormatGuidanceTool.Description,
                serializerOptions: JsonOptions);

        ChatOptions options = new()
        {
            MaxOutputTokens = 3_500,
            ToolMode = ChatToolMode.Auto,
            Tools =
            [
                formatGuidanceTool
            ]
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

        if (guidanceState.InvocationCount ==
            invocationCountBeforeRequest)
        {
            logger.LogWarning(
                "The planner completed {OperationName} for episode " +
                "{EpisodeId} without requesting the required " +
                "episode-format guidance tool.",
                operationName,
                episodeId);

            throw new InvalidOperationException(
                $"The planner completed {operationName} without " +
                "retrieving current episode-format guidance.");
        }

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

    private static EpisodeFormatGuidance GetSelectedGuidance(
        EpisodeFormatGuidanceState guidanceState)
    {
        return guidanceState.Guidance ??
               throw new InvalidOperationException(
                   "No episode-format guidance was selected.");
    }

    private sealed class EpisodeFormatGuidanceState
    {
        public EpisodeFormatGuidance? Guidance { get; set; }

        public int InvocationCount { get; set; }
    }
}
