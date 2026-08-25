using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>The counsellor's call list.</summary>
[ApiController]
[Route("followups")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class FollowupController : ControllerBase
{
    private readonly IFollowupService _service;
    private readonly ILogger<FollowupController> _logger;

    public FollowupController(IFollowupService service, ILogger<FollowupController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Today and earlier — the actual call list.</summary>
    [HttpGet("due")]
    public async Task<ActionResult<IEnumerable<FollowupDto>>> GetDue(
        [FromQuery] bool? mine, CancellationToken ct) =>
        Ok(await _service.GetDueAsync(User.ResolveScope(mine), ct));

    /// <summary>Booked for a future date, deliberately separate.</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<FollowupDto>>> GetUpcoming(
        [FromQuery] bool? mine, CancellationToken ct) =>
        Ok(await _service.GetUpcomingAsync(User.ResolveScope(mine), ct));

    [HttpGet("by-inquiry/{inquiryId:int}")]
    public async Task<ActionResult<IEnumerable<FollowupDto>>> GetByInquiry(
        int inquiryId, CancellationToken ct) =>
        Ok(await _service.GetByInquiryAsync(inquiryId, ct));

    [HttpPut("{id:int}/log")]
    public async Task<ActionResult<FollowupDto>> Log(
        int id, FollowupLogRequest request, CancellationToken ct)
    {
        var dto = await _service.LogAsync(id, request, ct);
        return dto is null ? NotFound(new ApiError(404, $"Follow-up {id} was not found")) : Ok(dto);
    }

    /// <summary>
    /// Manual scheduling, for an open enquiry that has no pending call —
    /// which happens when it arrived with no active counsellor to assign to.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FollowupDto>> Schedule(
        [FromQuery] int inquiryId,
        [FromQuery] int staffId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var dto = await _service.ScheduleAsync(inquiryId, staffId, date, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
