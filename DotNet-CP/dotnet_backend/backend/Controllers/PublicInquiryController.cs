using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// The public website's enquiry form — one endpoint, in its own controller.
///
/// It lives here rather than on InquiryController because of how ASP.NET Core
/// combines authorisation attributes: they are ANDed, never overridden. A
/// [Authorize] on an action inside a class already carrying
/// [Authorize(Roles = "Admin,Manager,Counselor,Receptionist")] would require
/// BOTH — so a visitor with a valid Google token would still be refused, and
/// the only attribute that escapes the class rule is [AllowAnonymous], which
/// opens it to everyone.
///
/// Splitting the action out is the honest fix. The alternative — keeping it
/// on InquiryController with [AllowAnonymous] and hand-checking
/// User.Identity.IsAuthenticated in the method body — works, but it moves an
/// access rule out of the framework's hands and into code that nothing
/// audits.
///
/// This mirrors the Java SecurityConfig, where the same endpoint is
/// .authenticated() rather than .hasRole("VISITOR").
/// </summary>
[ApiController]
[Route("inquiries")]
[Authorize]
public class PublicInquiryController : ControllerBase
{
    private readonly IInquiryService _service;
    private readonly ILogger<PublicInquiryController> _logger;

    public PublicInquiryController(IInquiryService service, ILogger<PublicInquiryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Files an enquiry from the public site.
    ///
    /// Authenticated, not role-restricted, and that is deliberate on both
    /// backends: a staff member testing the public form carries a staff token,
    /// which satisfies [Authorize] but would fail a VISITOR role check. Gating
    /// on the role would block the people most likely to be checking the page
    /// works.
    /// </summary>
    [HttpPost("public")]
    public async Task<ActionResult<InquiryDto>> CreateFromWebsite(
        InquiryCreateRequest request, CancellationToken ct)
    {
        // The enquirer's email is taken from the request, matching Java. The
        // token proves somebody signed in; it is the form that says who the
        // enquiry is about, and those are not always the same person — a
        // parent enquiring for a child is the ordinary case.
        _logger.LogInformation(
            "Public enquiry submitted by signed-in visitor {Visitor}",
            User.Identity?.Name ?? "unknown");

        var created = await _service.CreateAsync(request, "Website", ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}
