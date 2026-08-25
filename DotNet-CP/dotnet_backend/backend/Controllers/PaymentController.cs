using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>Fee collection and receipts.</summary>
[ApiController]
[Route("payments")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly JavaMicroserviceClient _javaClient;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService payments,
        JavaMicroserviceClient javaClient,
        ILogger<PaymentController> logger)
    {
        _payments = payments;
        _javaClient = javaClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Collect(PaymentRequest request, CancellationToken ct)
    {
        var payment = await _payments.CollectAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, payment);
    }

    /// <summary>What is still owed on an enrolment.</summary>
    [HttpGet("enrollments/{enrollmentId:int}/fees")]
    public async Task<ActionResult<FeeBreakdownDto>> FeeStatus(int enrollmentId, CancellationToken ct) =>
        Ok(await _payments.FeesForEnrollmentAsync(enrollmentId, ct));

    /// <summary>
    /// The receipt PDF — requirement 11, and the reason it is worth having.
    ///
    /// Rendering is delegated to the Java service because OpenPDF is a Java
    /// library. Reimplementing that layout in C# would be duplicated work
    /// with two sets of bugs and a second PDF library to license, whereas
    /// "one service owns document rendering" is a real capability boundary.
    ///
    /// The caller's bearer token is forwarded, since the Java endpoint is
    /// authenticated — a receipt carries a student's name, fees and contact
    /// details, so it is not a public document.
    ///
    /// This is the one endpoint in the .NET backend that needs Java running.
    /// It returns 503, not 500: nothing here is broken, a dependency is
    /// absent, and that is a different thing for a caller to act on.
    /// </summary>
    [HttpGet("{paymentId:int}/receipt")]
    public async Task<IActionResult> Receipt(int paymentId, CancellationToken ct)
    {
        var token = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var pdf = await _javaClient.GetReceiptPdfAsync(paymentId, token, ct);

        if (pdf is null)
        {
            _logger.LogWarning("Receipt {PaymentId} could not be produced", paymentId);
            return StatusCode(503, new ApiError(503,
                "Receipt rendering is handled by the Java service and it didn't return one. " +
                "Check that it's running on port 8080 and that both backends share the same JWT secret."));
        }

        // Inline rather than an attachment: the React client opens it as a
        // blob URL in a new tab.
        return File(pdf, "application/pdf");
    }
}
