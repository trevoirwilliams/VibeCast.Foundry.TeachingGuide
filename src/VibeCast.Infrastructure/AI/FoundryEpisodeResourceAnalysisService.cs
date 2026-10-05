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

    public Task<EpisodeResourceAnalysisResult> AnalyzeAsync(Guid episodeId, Guid mediaAssetId, string ownerId, CancellationToken cancellationToken = default)
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
            ?? throw new KeyNotFoundException("The episode could not be found.");

        EpisodePlan acceptedPlan = episode.AcceptedPlan?.Plan
            ?? throw new InvalidOperationException(
                "Save an accepted episode plan before " +
                "assessing supporting resources.");

        MediaAssetSummary source = episode.MediaAssets.SingleOrDefault(asset => asset.Id == mediaAssetId)
            ?? throw new KeyNotFoundException("The selected resource is not attached " +
                "to this episode.");

        if (!MediaAssetHelpers.IsSupportedDocumentType(source.ContentType))
        {
            throw new InvalidOperationException(
                "Only attached PDF and text resources " +
                "can be assessed in this workflow.");
        }

        MediaContent media = await mediaAssetService.OpenMediaAsync(
                mediaAssetId,
                ownerId,
                cancellationToken)
            ?? throw new KeyNotFoundException("The selected resource could not be opened.");

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
        // PRACTICE S06-04: Use the prepared relevance prompt and extracted document context to request a typed assessment. Reject unusable completions and preserve cancellation.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S06-04: implement AssessRelevanceAsync.");
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
