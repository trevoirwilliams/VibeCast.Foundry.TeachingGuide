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

        // PRACTICE S06-01: RequestArtworkAnalysisAsync
        // 1. Load the supplied image stream with its content type.
        // 2. Build a system message and a user message containing episode context and image data.
        // 3. Request typed ArtworkAnalysis with the section output limit and caller token.
        // 4. Return the SDK response; the surrounding method validates and maps it.
        Task<ChatResponse<ArtworkAnalysis>> RequestArtworkAnalysisAsync()
        {
            // Optional API hints and checks: docs/practice/README.md#s06-01-requestartworkanalysisasync
            throw new NotImplementedException("S06-01: implement RequestArtworkAnalysisAsync.");
        }

        ChatResponse<ArtworkAnalysis> response = await RequestArtworkAnalysisAsync();

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
