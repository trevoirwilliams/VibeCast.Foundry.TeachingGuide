using VibeCast.Domain.Media;

namespace VibeCast.Application.Media;

public interface IMediaAssetService
{
    Task<IReadOnlyList<MediaAssetSummary>> ListAsync( string ownerId,
        CancellationToken cancellationToken = default);
    Task<MediaUploadResult> UploadAsync(MediaUploadRequest request, Stream content, string ownerId, CancellationToken cancellationToken = default);
}
