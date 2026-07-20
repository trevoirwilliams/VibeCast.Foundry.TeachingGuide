using VibeCast.Application.Validation;

namespace VibeCast.Application.Media;

public sealed record MediaUploadRequest(string FileName, string ContentType, long SizeBytes, Guid? EpisodeId);

public sealed class MediaUploadValidator : IValidator<MediaUploadRequest>
{
    public const long MaximumSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".m4a", ".mp4", ".png", ".jpg", ".jpeg", ".pdf", ".txt" };

    public ValidationResult Validate(MediaUploadRequest instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var result = new ValidationResult();
        var extension = Path.GetExtension(instance.FileName);

        if (string.IsNullOrWhiteSpace(instance.FileName) || !AllowedExtensions.Contains(extension))
        {
            result.Add(nameof(instance.FileName), "The selected file type is not supported.");
        }

        if (instance.SizeBytes <= 0 || instance.SizeBytes > MaximumSizeBytes)
        {
            result.Add(nameof(instance.SizeBytes), "The file must be larger than zero and no more than 25 MB.");
        }

        if (string.IsNullOrWhiteSpace(instance.ContentType))
        {
            result.Add(nameof(instance.ContentType), "A content type is required.");
        }

        return result;
    }
}
