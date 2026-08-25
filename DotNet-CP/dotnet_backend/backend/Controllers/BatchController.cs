using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Batch management.
///
/// Not built on GenericController, because current_count is system-driven:
/// every read replaces the cached column with a figure derived from live
/// enrolments, and no write is allowed to set it at all.
/// </summary>
[ApiController]
[Route("batches")]
[Authorize(Roles = "Admin,Manager")]
public class BatchController : ControllerBase
{
    private readonly IBatchService _service;

    public BatchController(IBatchService service) => _service = service;

    /// <summary>Public list, for the placement pages.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BatchDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    /// <summary>
    /// Carries staff assignments and capacity, so unlike the plain list this
    /// one is authenticated. Same split as the Java SecurityConfig, which
    /// claims /batches/detailed before the public /batches/* rule.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<ActionResult<IEnumerable<BatchDto>>> GetDetailed(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpGet("completed-for-placement")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BatchDto>>> GetCompleted(CancellationToken ct) =>
        Ok(await _service.GetCompletedAsync(ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<BatchDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return dto is null ? NotFound(new ApiError(404, $"Batch {id} was not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<BatchDto>> Create(BatchRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BatchDto>> Update(int id, BatchRequest request, CancellationToken ct)
    {
        var dto = await _service.UpdateAsync(id, request, ct);
        return dto is null ? NotFound(new ApiError(404, $"Batch {id} was not found")) : Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await _service.DeleteAsync(id, ct)
            ? NoContent()
            : NotFound(new ApiError(404, $"Batch {id} was not found"));
}
