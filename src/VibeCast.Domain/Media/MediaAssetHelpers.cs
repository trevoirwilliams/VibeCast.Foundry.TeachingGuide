namespace VibeCast.Domain.Media;

public static class MediaAssetHelpers
{
    public static bool IsSupportedArtwork(string contentType) => IsImageContentType(contentType);

    public static string GetTypeCode(string contentType)
    {
        string normalized = NormalizeContentType(contentType);

        return normalized switch
        {
            "image/png" or "image/jpeg" => "IM",
            "audio/wav" or "audio/mpeg" or "audio/mp4" => "AU",
            "video/mp4" => "VI",
            "text/plain" => "TXT",
            "application/pdf" => "PDF",
            _ => "DOC"
        };
    }

    public static string GetPreviewClass(string contentType)
    {
        string normalized = NormalizeContentType(contentType);

        return normalized switch
        {
            "image/png" or "image/jpeg" => "preview-image",
            "audio/wav" or "audio/mpeg" or "audio/mp4" => "preview-audio",
            "video/mp4" => "preview-video",
            _ => "preview-document"
        };
    }

    public static bool IsArtwork(string contentType) => IsImageContentType(contentType);

    public static string GetMediaSummary(string contentType)
    {
        string normalized = NormalizeContentType(contentType);

        return normalized switch
        {
            "image/png" or "image/jpeg" => "Artwork or cover image ready for review.",
            "audio/wav" or "audio/mpeg" or "audio/mp4" => "Audio source ready for transcription.",
            "video/mp4" => "Video source ready for extraction.",
            "application/pdf" => "Supporting document attached for planning.",
            "text/plain" => "Text reference attached for the episode.",
            _ => "Supporting media attached for this episode."
        };
    }

    public static string FormatSize(long bytes) =>
        bytes < 1_048_576
            ? $"{bytes / 1024d:N1} KB"
            : $"{bytes / 1_048_576d:N1} MB";

    public static bool IsAudio(string contentType) =>
    NormalizeContentType(contentType) is
        "audio/wav" or
        "audio/mpeg" or
        "audio/mp4";

    public static bool IsSupportedDocumentType(string contentType) =>
    NormalizeContentType(contentType) is
        "application/pdf" or
        "text/plain";

    private static bool IsImageContentType(string contentType) => NormalizeContentType(contentType) is "image/png" or "image/jpeg";

    private static string NormalizeContentType(string contentType) => contentType?.Trim().ToLowerInvariant() ?? string.Empty;
}
