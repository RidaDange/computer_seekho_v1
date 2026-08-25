using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>The lead-to-student conversion.</summary>
public interface IRegistrationService
{
    /// <summary>Step 1 — enquiries that can still be registered against.</summary>
    Task<IEnumerable<InquiryDto>> SearchRegisterableAsync(string? query, CancellationToken ct = default);

    /// <summary>Joinable batches for a course.</summary>
    Task<IEnumerable<BatchDto>> JoinableBatchesForCourseAsync(int courseId, CancellationToken ct = default);

    Task<FeeBreakdownDto> FeeBreakdownForCourseAsync(int courseId, CancellationToken ct = default);

    /// <param name="bearerToken">
    /// The caller's JWT, forwarded to the Java service to render the receipt
    /// PDF that gets attached to the confirmation email. Optional — without
    /// it the confirmation is still sent, just without the attachment.
    /// </param>
    Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request, string? bearerToken = null, CancellationToken ct = default);
}
