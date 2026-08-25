using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>One photo album per batch.</summary>
public interface IBatchAlbumService
{
    /// <summary>The batch's album, created on first use.</summary>
    Task<BatchAlbumDto> GetOrCreateForBatchAsync(int batchId, CancellationToken ct = default);

    Task<BatchAlbumDto> AddImageAsync(int batchId, BatchAlbumImageRequest request, int? uploadedBy, CancellationToken ct = default);
    Task<BatchAlbumDto> SetCoverAsync(int batchId, int imageId, CancellationToken ct = default);
    Task<BatchAlbumDto> RemoveImageAsync(int batchId, int imageId, CancellationToken ct = default);

    /// <summary>Albums with at least one photo, for the public strip.</summary>
    Task<IEnumerable<BatchAlbumSummaryDto>> GetPublishedAsync(CancellationToken ct = default);
}
