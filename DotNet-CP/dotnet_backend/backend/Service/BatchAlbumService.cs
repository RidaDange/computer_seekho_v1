using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Service;

/// <summary>
/// Batch albums.
///
/// The cover references an image row rather than a URL, so the interesting
/// rules here are all about keeping that reference honest.
/// </summary>
public class BatchAlbumService : IBatchAlbumService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<BatchAlbumService> _logger;

    public BatchAlbumService(AppDbContext db, IMapper mapper, ILogger<BatchAlbumService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BatchAlbumDto> GetOrCreateForBatchAsync(int batchId, CancellationToken ct = default)
    {
        var album = await _db.BatchAlbums.FirstOrDefaultAsync(a => a.BatchId == batchId, ct);

        if (album is null)
        {
            if (!await _db.Batches.AnyAsync(b => b.BatchId == batchId, ct))
            {
                throw new ResourceNotFoundException("Batch", batchId);
            }

            // Created lazily on first view rather than alongside the batch.
            // The consequence — an empty album means "somebody looked", not
            // "there is something to see" — is why the public strip filters
            // out albums with no photos.
            album = new BatchAlbum { BatchId = batchId, Title = "Batch Photos", IsActive = true };
            _db.BatchAlbums.Add(album);
            await _db.SaveChangesAsync(ct);
        }

        return await LoadAsync(album.AlbumId, ct);
    }

    public async Task<BatchAlbumDto> AddImageAsync(
        int batchId, BatchAlbumImageRequest request, int? uploadedBy, CancellationToken ct = default)
    {
        var album = await RequireAlbumAsync(batchId, ct);

        var image = new BatchAlbumImage
        {
            AlbumId = album.AlbumId,
            ImageUrl = request.ImageUrl.Trim(),
            Caption = request.Caption,
            UploadedBy = uploadedBy,
            DisplayOrder = request.DisplayOrder,
            UploadDate = DateOnly.FromDateTime(DateTime.Today),
            IsActive = true
        };

        _db.BatchAlbumImages.Add(image);
        await _db.SaveChangesAsync(ct);

        // The first photo becomes the cover, so an album is never left
        // showing nothing on the public strip when it clearly has content.
        if (album.CoverImageId is null)
        {
            album.CoverImageId = image.ImageId;
            await _db.SaveChangesAsync(ct);
        }

        return await LoadAsync(album.AlbumId, ct);
    }

    public async Task<BatchAlbumDto> SetCoverAsync(int batchId, int imageId, CancellationToken ct = default)
    {
        var album = await RequireAlbumAsync(batchId, ct);

        // The cover must belong to THIS album. Without the check you can set
        // one batch's photo as another batch's cover, which is exactly the
        // drift the cover_image_id foreign key was introduced to prevent.
        var belongs = await _db.BatchAlbumImages
            .AnyAsync(i => i.ImageId == imageId && i.AlbumId == album.AlbumId, ct);

        if (!belongs)
        {
            throw new BusinessRuleException("That image isn't in this batch's album.");
        }

        album.CoverImageId = imageId;
        await _db.SaveChangesAsync(ct);

        return await LoadAsync(album.AlbumId, ct);
    }

    public async Task<BatchAlbumDto> RemoveImageAsync(int batchId, int imageId, CancellationToken ct = default)
    {
        var album = await RequireAlbumAsync(batchId, ct);

        var image = await _db.BatchAlbumImages
            .FirstOrDefaultAsync(i => i.ImageId == imageId && i.AlbumId == album.AlbumId, ct)
            ?? throw new ResourceNotFoundException("Album image", imageId);

        _db.BatchAlbumImages.Remove(image);

        // Deleting the cover promotes the next photo rather than leaving the
        // album coverless — otherwise removing one picture would silently
        // drop the whole album off the public strip.
        if (album.CoverImageId == imageId)
        {
            var next = await _db.BatchAlbumImages
                .Where(i => i.AlbumId == album.AlbumId && i.ImageId != imageId)
                .OrderBy(i => i.DisplayOrder).ThenBy(i => i.ImageId)
                .FirstOrDefaultAsync(ct);

            album.CoverImageId = next?.ImageId;
        }

        await _db.SaveChangesAsync(ct);
        return await LoadAsync(album.AlbumId, ct);
    }

    public async Task<IEnumerable<BatchAlbumSummaryDto>> GetPublishedAsync(CancellationToken ct = default)
    {
        var albums = await _db.BatchAlbums.AsNoTracking()
            .Include(a => a.Batch)
            .Where(a => a.IsActive)
            .ToListAsync(ct);

        var ids = albums.Select(a => a.AlbumId).ToList();

        var images = await _db.BatchAlbumImages.AsNoTracking()
            .Where(i => ids.Contains(i.AlbumId) && i.IsActive)
            .ToListAsync(ct);

        return albums
            // Albums with no photos are filtered out. Because the admin side
            // creates the row the moment someone opens the Album tab, an
            // empty album means "somebody looked", not "there is something
            // to see".
            .Where(a => images.Any(i => i.AlbumId == a.AlbumId))
            .Select(a =>
            {
                var own = images.Where(i => i.AlbumId == a.AlbumId).ToList();
                var cover = own.FirstOrDefault(i => i.ImageId == a.CoverImageId)
                            ?? own.OrderBy(i => i.DisplayOrder).ThenBy(i => i.ImageId).First();

                return new BatchAlbumSummaryDto(
                    a.AlbumId, a.BatchId, a.Batch?.BatchName, a.Title, cover.ImageUrl, own.Count);
            })
            .ToList();
    }

    // ------------------------------------------------------------ helpers

    private async Task<BatchAlbum> RequireAlbumAsync(int batchId, CancellationToken ct)
    {
        await GetOrCreateForBatchAsync(batchId, ct);
        return await _db.BatchAlbums.FirstAsync(a => a.BatchId == batchId, ct);
    }

    private async Task<BatchAlbumDto> LoadAsync(int albumId, CancellationToken ct)
    {
        var album = await _db.BatchAlbums.AsNoTracking()
            .Include(a => a.Batch)
            .FirstAsync(a => a.AlbumId == albumId, ct);

        var images = await _db.BatchAlbumImages.AsNoTracking()
            .Where(i => i.AlbumId == albumId && i.IsActive)
            .OrderBy(i => i.DisplayOrder).ThenBy(i => i.ImageId)
            .ToListAsync(ct);

        var dtos = images
            .Select(i => _mapper.Map<BatchAlbumImageDto>(i) with { IsCover = i.ImageId == album.CoverImageId })
            .ToList();

        return new BatchAlbumDto
        {
            AlbumId = album.AlbumId,
            BatchId = album.BatchId,
            BatchName = album.Batch?.BatchName,
            Title = album.Title,
            Description = album.Description,
            CoverImageId = album.CoverImageId,
            CoverImageUrl = dtos.FirstOrDefault(i => i.IsCover)?.ImageUrl,
            Images = dtos
        };
    }
}
