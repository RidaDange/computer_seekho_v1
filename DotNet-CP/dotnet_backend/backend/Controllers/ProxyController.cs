using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Requirement 11 — demonstrates the .NET backend calling the Java service.
///
/// Kept on its own route rather than woven into the business endpoints,
/// because the .NET API is meant to run the whole system independently.
/// Java is an integration to reach for, not something the rest of this
/// application needs in order to work.
/// </summary>
[ApiController]
[Route("java")]
public class ProxyController : ControllerBase
{
    private readonly JavaMicroserviceClient _javaClient;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(JavaMicroserviceClient javaClient, ILogger<ProxyController> logger)
    {
        _javaClient = javaClient;
        _logger = logger;
    }

    /// <summary>
    /// Forwards a read to the Java service and returns its response as-is.
    /// The simplest possible proof that the HttpClient wiring works.
    /// </summary>
    [HttpGet("courses")]
    [AllowAnonymous]
    public async Task<IActionResult> ForwardCourses(CancellationToken ct)
    {
        var json = await _javaClient.GetActiveCoursesAsync(ct);

        return json is null
            // 503, not 500: nothing here is broken. A dependency is
            // unreachable, and that is a different thing for a caller to act
            // on than a bug in this service.
            ? StatusCode(503, new ApiError(503, "The Java service is not reachable"))
            : Content(json, "application/json");
    }

    /// <summary>
    /// Delegates receipt rendering to Java, which owns it because OpenPDF is
    /// a Java library. The caller's bearer token is forwarded, since the
    /// receipt endpoint over there is authenticated.
    /// </summary>
    [HttpGet("receipts/{paymentId:int}")]
    [Authorize]
    public async Task<IActionResult> Receipt(int paymentId, CancellationToken ct)
    {
        var token = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var pdf = await _javaClient.GetReceiptPdfAsync(paymentId, token, ct);

        return pdf is null
            ? StatusCode(503, new ApiError(503, "The Java service is not reachable"))
            : File(pdf, "application/pdf", $"receipt-{paymentId}.pdf");
    }
}
