using ComputerSeekho.Models;

namespace ComputerSeekho.Service;

/// <summary>
/// Decides which counsellor a new enquiry goes to.
///
/// Its own service rather than a private method on InquiryService, for the
/// same reason it is on the Java side: it is the one piece of scheduling
/// logic in the system, and it is worth being able to test in isolation.
/// </summary>
public interface ICounselorAssignmentService
{
    /// <summary>
    /// The next counsellor to assign to, or null when no active counsellor
    /// exists. Null is a legitimate answer, not an error — an enquiry
    /// arriving out of hours still has to be captured.
    /// </summary>
    Task<Staff?> NextCounselorAsync(CancellationToken ct = default);
}
