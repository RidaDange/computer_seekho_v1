using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

/// <summary>
/// An enquiry as the API exposes it.
///
/// Init-only properties rather than a positional record, for the same
/// reason CourseDto is: several members are resolved from other tables and
/// have no counterpart on the Inquiry row, and AutoMapper cannot satisfy a
/// constructor parameter that has no source.
///
/// Property names match the Java InquiryDto exactly — the React screens
/// read i.enquirerName, i.courseName, i.staffName and i.status directly.
/// </summary>
public record InquiryDto
{
    public int InquiryId { get; init; }
    public int CourseId { get; init; }
    public string? CourseName { get; init; }
    public int? StaffId { get; init; }
    public string? StaffName { get; init; }
    public string EnquirerName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Message { get; init; }
    public string? Source { get; init; }

    /// <summary>The database spelling — "In-Followup", not "InFollowup".
    /// React compares against these strings.</summary>
    public string? Status { get; init; }

    public DateOnly InquiryDate { get; init; }
    public int? ClosureReasonId { get; init; }
    public string? ClosureReasonText { get; init; }

    /// <summary>Next pending call, resolved from followups. Null when the
    /// enquiry has none — which is what the Schedule link on the enquiry
    /// list exists to recover from.</summary>
    public DateOnly? NextFollowupDate { get; init; }
}

/// <summary>
/// The enquiry form's payload, used by both entry channels — the public
/// website and the staff Add Enquiry screen.
///
/// Validation lives here so both inherit it. staffId is deliberately
/// absent: the counsellor is decided by the server's least-loaded round
/// robin, never posted by the client.
/// </summary>
public class InquiryCreateRequest
{
    [Required(ErrorMessage = "Please select a course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
    public string EnquirerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[6-9]\d{9}$",
        ErrorMessage = "Please enter a valid 10-digit Indian mobile number")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Message is too long")]
    public string? Message { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }
}

/// <summary>Closing an enquiry. The reason is mandatory and comes from the
/// fixed closure_reasons list — free text is deliberately impossible.</summary>
public class CloseInquiryRequest
{
    [Required(ErrorMessage = "An outcome is required")]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "A closure reason is required")]
    public int ClosureReasonId { get; set; }
}
