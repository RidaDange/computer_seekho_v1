using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// The registration wizard's three steps, plus the commit.
///
/// Its own controller rather than a method on StudentController, because
/// registering is not "creating a student" — it is one transaction across
/// students, enrollments, payments, batches and inquiries, and giving it
/// its own path makes that visible.
/// </summary>
[ApiController]
[Route("registrations")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _service;

    public RegistrationController(IRegistrationService service) => _service = service;

    [HttpGet("eligible-inquiries")]
    public async Task<ActionResult<IEnumerable<InquiryDto>>> SearchInquiries(
        [FromQuery] string? q, CancellationToken ct) =>
        Ok(await _service.SearchRegisterableAsync(q, ct));

    [HttpGet("courses/{courseId:int}/batches")]
    public async Task<ActionResult<IEnumerable<BatchDto>>> JoinableBatches(
        int courseId, CancellationToken ct) =>
        Ok(await _service.JoinableBatchesForCourseAsync(courseId, ct));

    [HttpGet("courses/{courseId:int}/fees")]
    public async Task<ActionResult<FeeBreakdownDto>> Fees(int courseId, CancellationToken ct) =>
        Ok(await _service.FeeBreakdownForCourseAsync(courseId, ct));

    [HttpPost]
    public async Task<ActionResult<RegistrationResult>> Register(
        RegistrationRequest request, CancellationToken ct)
    {
        // Forwarded so the service can ask the Java backend to render the
        // receipt PDF for the confirmation email. Java authorises that call
        // independently, so it needs a real token — the same one the
        // counsellor is already holding.
        var token = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var result = await _service.RegisterAsync(request, token, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
