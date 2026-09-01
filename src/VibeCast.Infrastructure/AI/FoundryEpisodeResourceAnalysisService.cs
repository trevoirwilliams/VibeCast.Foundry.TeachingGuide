using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Azure;
using Azure.AI.ContentUnderstanding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Common;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Domain.Episodes;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodeResourceAnalysisService(
    ContentUnderstandingClient contentUnderstandingClient,
    IChatClient chatClient,
    IEpisodeService episodeService,
    IMediaAssetService mediaAssetService,
    IDbContextFactory<VibeCastDbContext> dbContextFactory,
    IValidator<SupportingSourceAssessmentValidationRequest>
        assessmentValidator,
    ILogger<FoundryEpisodeResourceAnalysisService> logger) : IEpisodeResourceAnalysisService
{
    private const string AnalyzerId ="prebuilt-documentSearch";

    private const int MaximumSemanticContextCharacters = 24_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public async Task<EpisodeResourceAnalysisResult> AnalyzeAsync(Guid episodeId, Guid mediaAssetId, string ownerId, CancellationToken cancellationToken = default)
    {
        if (episodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid episode identifier is required.",
                nameof(episodeId));
        }

        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid media asset identifier is required.",
                nameof(mediaAssetId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        EpisodeDetails episode = await episodeService.GetAsync(
                episodeId,
                ownerId,
                cancellationToken)
            ?? throw new SafeApplicationException("The episode could not be found.");

        EpisodePlan acceptedPlan = episode.AcceptedPlan?.Plan
            ?? throw new SafeApplicationException(
                "Save an accepted episode plan before " +
                "assessing supporting resources.");

        MediaAssetSummary source = episode.MediaAssets.SingleOrDefault(asset => asset.Id == mediaAssetId)
            ?? throw new SafeApplicationException("The selected resource is not attached " +
                "to this episode.");

        if (!MediaAssetHelpers.IsSupportedDocumentType(source.ContentType))
        {
            throw new SafeApplicationException(
                "Only attached PDF and text resources " +
                "can be assessed in this workflow.");
        }

        MediaContent media = await mediaAssetService.OpenMediaAsync(
                mediaAssetId,
                ownerId,
                cancellationToken)
            ?? throw new SafeApplicationException("The selected resource could not be opened.");

        await using Stream sourceStream = media.Content;

        using MemoryStream buffer = new();

        await sourceStream.CopyToAsync(buffer, cancellationToken);

        BinaryData binaryInput = BinaryData.FromBytes(buffer.ToArray());


        Operation<AnalysisResult> operation = await contentUnderstandingClient
                .AnalyzeBinaryAsync(
                    WaitUntil.Completed,
                    AnalyzerId,
                    binaryInput,
                    contentType: media.ContentType,
                    cancellationToken: cancellationToken);

        AnalysisResult analysis = operation.Value;

        string analyzedContent = BuildSemanticContext(analysis);

        SupportingSourceAssessment assessment = await AssessRelevanceAsync(
            episode,
            source,
            analyzedContent,
            cancellationToken);

        ValidationResult validation = assessmentValidator.Validate(
                new SupportingSourceAssessmentValidationRequest(assessment,
                    acceptedPlan.EvidenceRequirements));

        if (!validation.IsValid)
        {
            string[] paths = validation.Errors
                    .Select(error => error.PropertyName)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            logger.LogWarning(
                "Supporting resource assessment failed " +
                "validation for episode {EpisodeId} and " +
                "media asset {MediaAssetId}. " +
                "FailurePaths: {FailurePaths}.",
                episodeId,
                mediaAssetId,
                paths);

            throw new InvalidOperationException(
                "The resource assessment did not satisfy " +
                "VibeCast validation rules.");
        }

        return await PersistDecisionAsync(
            episode,
            source,
            assessment,
            ownerId,
            cancellationToken);
    }

    private async Task<EpisodeResourceAnalysisResult> PersistDecisionAsync(
        EpisodeDetails episode,
        MediaAssetSummary source,
        SupportingSourceAssessment assessment,
        string ownerId,
        CancellationToken cancellationToken)
    {
        await using VibeCastDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        EpisodeSupportingSource? existing = await dbContext.EpisodeSupportingSources
                .SingleOrDefaultAsync(candidate =>
                        candidate.MediaAssetId == source.Id &&
                        candidate.OwnerId == ownerId,
                    cancellationToken);

        if (!assessment.IsRelevant)
        {
            if (existing is not null)
            {
                dbContext.EpisodeSupportingSources.Remove(existing);

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Media asset {MediaAssetId} was assessed " +
                "as not relevant to episode {EpisodeId}.",
                source.Id,
                episode.Id);

            return new EpisodeResourceAnalysisResult(
                IsRelevant: false,
                Message: $"{source.OriginalFileName} does not " +
                    "appear semantically useful to the " +
                    "accepted episode plan and was not added.",
                SupportingSource: null);
        }

        DateTimeOffset analyzedAtUtc = DateTimeOffset.UtcNow;

        string relevantPointsJson = JsonSerializer.Serialize(
                assessment.RelevantPoints,
                JsonOptions);

        string matchedEvidenceJson = JsonSerializer.Serialize(
                assessment.MatchedEvidenceRequirements,
                JsonOptions);

        if (existing is null)
        {
            existing = EpisodeSupportingSource.Create(
                    episodeId: episode.Id,
                    mediaAssetId: source.Id,
                    ownerId: ownerId,
                    summary: assessment.Summary,
                    relevanceRationale: assessment.Rationale,
                    relevantPointsJson: relevantPointsJson,
                    matchedEvidenceRequirementsJson: matchedEvidenceJson,
                    analyzerId: AnalyzerId,
                    relevancePromptVersion:EpisodeResourceRelevancePrompt.Version,
                    analyzedAtUtc: analyzedAtUtc);

            dbContext.EpisodeSupportingSources
                .Add(existing);
        }
        else
        {
            existing.RefreshAnalysis(
                summary: assessment.Summary,
                relevanceRationale: assessment.Rationale,
                relevantPointsJson: relevantPointsJson,
                matchedEvidenceRequirementsJson: matchedEvidenceJson,
                analyzerId: AnalyzerId,
                relevancePromptVersion: EpisodeResourceRelevancePrompt.Version,
                analyzedAtUtc: analyzedAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        EpisodeSupportingSourceSummary sourceSummary =
            new(
                Id: existing.Id,
                MediaAssetId: source.Id,
                SourceFileName: source.OriginalFileName,
                ContentType: source.ContentType,
                Summary: assessment.Summary,
                Rationale: assessment.Rationale,
                RelevantPoints: assessment.RelevantPoints,
                MatchedEvidenceRequirements: assessment.MatchedEvidenceRequirements,
                AnalyzerId: AnalyzerId,
                RelevancePromptVersion: EpisodeResourceRelevancePrompt.Version,
                AnalyzedAtUtc: analyzedAtUtc);

        logger.LogInformation(
            "Media asset {MediaAssetId} was added as a " +
            "supporting source for episode {EpisodeId}. " +
            "AnalyzerId: {AnalyzerId}; PromptVersion: " +
            "{PromptVersion}.",
            source.Id,
            episode.Id,
            AnalyzerId,
            EpisodeResourceRelevancePrompt.Version);

        return new EpisodeResourceAnalysisResult(
            IsRelevant: true,
            Message:$"{source.OriginalFileName} was relevant " +
                "and has been added to Supporting Sources.",
            SupportingSource: sourceSummary);
    }

    private async Task<SupportingSourceAssessment>
        AssessRelevanceAsync(
            EpisodeDetails episode,
            MediaAssetSummary source,
            string analyzedContent,
            CancellationToken cancellationToken)
    {
        ChatMessage[] messages =
        [
            new(
                ChatRole.System,
                EpisodeResourceRelevancePrompt.SystemMessage
               ),

            new(
                ChatRole.User,
                EpisodeResourceRelevancePrompt.BuildUserMessage(
                        episode,
                        source,
                        analyzedContent)
                )
        ];

        ChatOptions options = new()
        {
            MaxOutputTokens = 10_500
        };

        ChatResponse<SupportingSourceAssessment> response = await chatClient
                .GetResponseAsync<SupportingSourceAssessment>(
                    messages,
                    JsonOptions,
                    options,
                    useJsonSchemaResponseFormat: true,
                    cancellationToken: cancellationToken);

        ChatResponseCompletionGuard.EnsureUsableCompletion(response.FinishReason,
                "supporting resource relevance assessment");

        if (!response.TryGetResult(out SupportingSourceAssessment?assessment) ||assessment is null)
        {
            throw new InvalidOperationException(
                "Microsoft Foundry did not return a " +
                "usable supporting-resource assessment.");
        }

        return assessment;
    }

    private static string BuildSemanticContext(AnalysisResult analysis)
    {
        AnalysisContent content = analysis.Contents?.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Content Understanding returned no " +
                "analyzed content.");

        string summary = content.Fields?.GetFieldOrDefault("Summary")?.Value?.ToString()?.Trim()
            ?? string.Empty;

        string markdown = content.Markdown?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidOperationException(
                "Content Understanding returned no " +
                "semantic document content.");
        }

        StringBuilder context = new();

        if (!string.IsNullOrWhiteSpace(summary))
        {
            context.AppendLine("CONTENT UNDERSTANDING SUMMARY");

            context.AppendLine(summary);

            context.AppendLine();
        }

        context.AppendLine("CONTENT UNDERSTANDING MARKDOWN");

        context.Append(markdown);

        string value = context.ToString();

        return value.Length <= MaximumSemanticContextCharacters
            ? value
            : value[..MaximumSemanticContextCharacters];
    }
}
