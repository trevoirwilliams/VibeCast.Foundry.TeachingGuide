using VibeCast.Application.Validation;
using VibeCast.Domain.Media;

namespace VibeCast.Application.Media;

public sealed record MediaUploadRequest(string FileName, string ContentType, long SizeBytes, Guid? EpisodeId);

public sealed record MediaAssetSummary(
    Guid Id,
    Guid? EpisodeId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    MediaAssetStatus Status,
    DateTimeOffset CreatedAtUtc);

public sealed record MediaUploadResult(
    MediaAssetSummary? Asset,
    IReadOnlyList<ValidationFailure> Errors)
{
    public bool Succeeded => Asset is not null;

    public static MediaUploadResult Accepted(
        MediaAssetSummary asset) =>
        new(asset, []);

    public static MediaUploadResult Rejected(
        IEnumerable<ValidationFailure> errors) =>
        new(null, errors.ToArray());

    public static MediaUploadResult Rejected(
        string propertyName,
        string errorMessage) =>
        new(
            null,
            [
                new ValidationFailure(
                    propertyName,
                    errorMessage)
            ]);
}

public sealed class MediaUploadValidator : IValidator<MediaUploadRequest>
{
    public const long MaximumSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".m4a", ".mp4", ".png", ".jpg", ".jpeg", ".pdf", ".txt" };

    public ValidationResult Validate(MediaUploadRequest instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var result = new ValidationResult();
        var displayName = GetDisplayName(instance.FileName);
        var extension = Path.GetExtension(displayName);

        if (string.IsNullOrWhiteSpace(instance.FileName) || !AllowedExtensions.Contains(extension))
        {
            result.Add(nameof(instance.FileName), "The selected file type is not supported.");
        }
        else if (instance.EpisodeId is null &&
                 !string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(
                nameof(instance.FileName),
                "Shared Source Gallery uploads must be PDF or TXT files.");
        }

        if (instance.SizeBytes <= 0 || instance.SizeBytes > MaximumSizeBytes)
        {
            result.Add(nameof(instance.SizeBytes), "The file must be larger than zero and no more than 25 MB.");
        }

        if (string.IsNullOrWhiteSpace(instance.ContentType))
        {
            result.Add(nameof(instance.ContentType), "A content type is required.");
        }

        if (instance.EpisodeId is Guid episodeId && episodeId == Guid.Empty)
        {
            result.Add(nameof(instance.EpisodeId), "The associated episode identifier is invalid.");
        }

        return result;
    }

    public static string GetDisplayName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        return Path.GetFileName(fileName.Replace('\\', '/'));
    }

    public static string GetCanonicalContentType(string fileName)
    {
        var extension = Path.GetExtension(GetDisplayName(fileName));

        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => throw new InvalidOperationException("The file type is not supported.")
        };
    }

    public async Task<bool> HasExpectedSignatureAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var header = await ReadHeaderAsync(content, 16, cancellationToken);
        var ext = Path.GetExtension(GetDisplayName(fileName)).ToLowerInvariant();

        return ext switch
        {
            ".png" => StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".jpg" or ".jpeg" => StartsWith(header, 0xFF, 0xD8, 0xFF),
            ".pdf" => StartsWith(header, 0x25, 0x50, 0x44, 0x46, 0x2D),
            ".wav" => IsWaveFile(header, header.Length),
            ".mp3" => IsMp3File(header),
            ".m4a" => HasMp4Brand(header, "M4A ", "mp4a", "isom"),
            ".mp4" => HasMp4Brand(header, "isom", "mp41", "mp42", "avc1"),
            ".txt" => IsLikelyTextFile(header),
            _ => false
        };
    }

    private static async Task<byte[]> ReadHeaderAsync(Stream content, int maxBytes, CancellationToken ct)
    {
        var buffer = new byte[maxBytes];
        var read = 0;

        while (read < buffer.Length)
        {
            var n = await content.ReadAsync(buffer.AsMemory(read), ct);
            if (n == 0) break;
            read += n;
        }

        if (read == buffer.Length) return buffer;
        return buffer[..read];
    }

    private static bool IsMp3File(byte[] h) =>
        StartsWith(h, 0x49, 0x44, 0x33) || // ID3
        (h.Length >= 2 && h[0] == 0xFF && (h[1] == 0xFB || h[1] == 0xF3 || h[1] == 0xF2));

    private static bool HasMp4Brand(byte[] h, params string[] brands)
    {
        if (h.Length < 12) return false;
        if (!(h[4] == (byte)'f' && h[5] == (byte)'t' && h[6] == (byte)'y' && h[7] == (byte)'p')) return false;

        foreach (var b in brands)
        {
            if (h[8] == (byte)b[0] && h[9] == (byte)b[1] && h[10] == (byte)b[2] && h[11] == (byte)b[3])
                return true;
        }

        return false;
    }

    private static bool IsLikelyTextFile(byte[] h)
    {
        if (StartsWith(h, 0xEF, 0xBB, 0xBF) || StartsWith(h, 0xFF, 0xFE) || StartsWith(h, 0xFE, 0xFF))
            return true;

        for (var i = 0; i < h.Length; i++)
        {
            if (h[i] == 0x00) return false;
        }

        return true;
    }

    private static bool IsWaveFile(
       byte[] header,
       int bytesRead) =>
       bytesRead >= 12 &&
       header.AsSpan(0, 4)
           .SequenceEqual("RIFF"u8) &&
       header.AsSpan(8, 4)
           .SequenceEqual("WAVE"u8);

    private static bool StartsWith(byte[] source, params byte[] sig)
    {
        if (source.Length < sig.Length) return false;

        for (var i = 0; i < sig.Length; i++)
        {
            if (source[i] != sig[i]) return false;
        }

        return true;
    }
}
