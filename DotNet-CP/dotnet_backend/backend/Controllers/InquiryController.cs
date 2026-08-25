using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Enquiries — capture, listing, closure and conversion.
/// </summary>
[ApiController]
[Route("inquiries")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class InquiryController : ControllerBase
{
    private readonly IInquiryService _service;
    private readonly ILogger<InquiryController> _logger;

    public InquiryController(IInquiryService service, ILogger<InquiryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Staff-entered enquiry — the walk-in at the campus desk.</summary>
    [HttpPost]
    public async Task<ActionResult<InquiryDto>> CreateByStaff(
        InquiryCreateRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, "Walk-in", ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Everything, including closed and converted, for history.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InquiryDto>>> GetAll(
        [FromQuery] bool? mine, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(User.ResolveScope(mine), ct));

    /// <summary>Open enquiries only.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<InquiryDto>>> GetActive(
        [FromQuery] bool? mine, CancellationToken ct) =>
        Ok(await _service.GetActiveAsync(User.ResolveScope(mine), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InquiryDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return dto is null ? NotFound(new ApiError(404, $"Enquiry {id} was not found")) : Ok(dto);
    }

    [HttpPut("{id:int}/close")]
    public async Task<ActionResult<InquiryDto>> Close(
        int id, CloseInquiryRequest request, CancellationToken ct)
    {
        var dto = await _service.CloseAsync(id, request, ct);
        return dto is null ? NotFound(new ApiError(404, $"Enquiry {id} was not found")) : Ok(dto);
    }

    /// <summary>
    /// Marks the enquiry Converted, which is what Student Registration hangs
    /// off — students.inquiry_id is NOT NULL, so an enquiry has to reach
    /// this state before anyone can be registered against it.
    /// </summary>
    [HttpPut("{id:int}/convert")]
    public async Task<ActionResult<InquiryDto>> Convert(int id, CancellationToken ct)
    {
        var dto = await _service.ConvertAsync(id, ct);
        return dto is null ? NotFound(new ApiError(404, $"Enquiry {id} was not found")) : Ok(dto);
    }
}
