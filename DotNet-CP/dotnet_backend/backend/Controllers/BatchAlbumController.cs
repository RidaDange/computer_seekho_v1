using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Nested under /batches because an album has no independent identity —
/// one per batch, and it lives inside the Batch Management screen.
/// </summary>
[ApiController]
[Route("batches/{batchId:int}/album")]
[Authorize(Roles = "Admin,Manager")]
public class BatchAlbumController : ControllerBase
{
    private readonly IBatchAlbumService _service;

    public BatchAlbumController(IBatchAlbumService service) => _service = service;

    /// <summary>Anonymous, because the public Batch Album page reads it.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<BatchAlbumDto>> Get(int batchId, CancellationToken ct) =>
        Ok(await _service.GetOrCreateForBatchAsync(batchId, ct));

    [HttpPost("images")]
    public async Task<ActionResult<BatchAlbumDto>> AddImage(
        int batchId, BatchAlbumImageRequest request, CancellationToken ct) =>
        Ok(await _service.AddImageAsync(batchId, request, User.StaffId(), ct));

    [HttpPut("cover/{imageId:int}")]
    public async Task<ActionResult<BatchAlbumDto>> SetCover(int batchId, int imageId, CancellationToken ct) =>
        Ok(await _service.SetCoverAsync(batchId, imageId, ct));

    [HttpDelete("images/{imageId:int}")]
    public async Task<ActionResult<BatchAlbumDto>> RemoveImage(int batchId, int imageId, CancellationToken ct) =>
        Ok(await _service.RemoveImageAsync(batchId, imageId, ct));
}

/// <summary>
/// The public Campus Life strip.
///
/// A separate controller because the route is not nested under a batch —
/// it lists every album that has something to show.
/// </summary>
[ApiController]
[Route("batch-albums")]
public class BatchAlbumListController : ControllerBase
{
    private readonly IBatchAlbumService _service;

    public BatchAlbumListController(IBatchAlbumService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BatchAlbumSummaryDto>>> GetPublished(CancellationToken ct) =>
        Ok(await _service.GetPublishedAsync(ct));
}
