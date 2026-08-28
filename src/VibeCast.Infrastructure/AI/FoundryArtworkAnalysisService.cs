using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Domain.Media;

namespace VibeCast.Infrastructure.AI;

public class FoundryArtworkAnalysisService(
    IChatClient chatClient,
    IValidator<ArtworkAnalysis> validator,
    ILogger<FoundryArtworkAnalysisService> logger)
    : IArtworkAnalysisService
{
    private const long MaximumImageSizeBytes = 20L * 1024L * 1024L;

    private static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    public async Task<ArtworkAnalysisResult> AnalyzeAsync(AnalyzeArtworkRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);

        ValidateRequest(request, content);

        DataContent imageContent =
            await DataContent.LoadFromAsync(
                content,
                request.ContentType,
                cancellationToken);

        ChatMessage[] messages =
        [
            new ChatMessage(
                ChatRole.System,
                ArtworkAnalysisPrompt.Instructions),

            new ChatMessage(
                ChatRole.User,
                new List<AIContent>
                {
                    new TextContent(
                        $$"""
                        Analyze this validated episode artwork.

                        Prompt version:
                        {{ArtworkAnalysisPrompt.Version}}

                        <episode-context>
                        Episode title:
                        {{request.EpisodeTitle}}

                        Original file name:
                        {{request.OriginalFileName}}
                        </episode-context>

                        The episode context explains the intended use of the artwork.

                        Do not claim that the episode title appears in the artwork unless it is clearly visible.
                        """),

                    imageContent
                })
        ];

        ChatOptions options = new()
        {
            MaxOutputTokens = 1_000
        };

        ChatResponse<ArtworkAnalysis> response =
            await chatClient.GetResponseAsync<ArtworkAnalysis>(
                    messages,
                    JsonOptions,
                    options,
                    useJsonSchemaResponseFormat: true,
                    cancellationToken: cancellationToken);

        ChatResponseCompletionGuard.EnsureUsableCompletion(response.FinishReason, "artwork analysis");

        if (!response.TryGetResult(out ArtworkAnalysis? analysis) || analysis is null)
        {
            logger.LogWarning(
                "Microsoft Foundry returned no usable " +
                "typed artwork analysis. ResponseId: " +
                "{ResponseId}; FinishReason: " +
                "{FinishReason}.",
                response.ResponseId,
                response.FinishReason);

            throw new InvalidOperationException(
                "Microsoft Foundry did not return a " +
                "usable artwork analysis.");
        }

        ArtworkAnalysis normalized = new(
                AltText: analysis.AltText?.Trim()
                    ?? string.Empty,

                VisualSummary: analysis.VisualSummary?.Trim()
                    ?? string.Empty,

                VisibleText: string.IsNullOrWhiteSpace(
                        analysis.VisibleText)
                        ? null
                        : analysis.VisibleText.Trim());

        ValidationResult validation = validator.Validate(normalized);

        if (!validation.IsValid)
        {
            string[] failurePaths = validation.Errors
                    .Select(error => error.PropertyName)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            logger.LogWarning(
                "The generated artwork analysis failed " +
                "application validation. FailurePaths: " +
                "{FailurePaths}.",
                failurePaths);

            throw new InvalidOperationException(
                "The generated artwork description did " +
                "not meet VibeCast accessibility rules.");
        }

        return new ArtworkAnalysisResult(
            Analysis: normalized, 
            PromptVersion: ArtworkAnalysisPrompt.Version,
            GeneratedAtUtc: DateTimeOffset.UtcNow
            );
    }

    private void ValidateRequest(AnalyzeArtworkRequest request, Stream content)
    {
        if (string.IsNullOrWhiteSpace(request.EpisodeTitle))
        {
            throw new ArgumentException(
                "An episode title is required.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            throw new ArgumentException(
                "An original file name is required.",
                nameof(request));
        }

        if (!MediaAssetHelpers.IsSupportedArtwork(request.ContentType))
        {
            throw new ArgumentException(
                "Only PNG and JPEG artwork can be " +
                "analyzed.",
                nameof(request));
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > MaximumImageSizeBytes)
        {
            throw new ArgumentException(
                "Artwork must be larger than zero and " +
                "no more than 20 MB.",
                nameof(request));
        }

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The artwork stream must be readable.",
                nameof(content));
        }
    }
}
