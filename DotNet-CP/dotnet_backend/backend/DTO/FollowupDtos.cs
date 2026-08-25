using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

/// <summary>
/// A follow-up call as the Follow-ups screen needs it.
///
/// Carries the enquiry's details flattened onto it, because the screen is a
/// call list: a counsellor needs the name, phone and course in the row
/// rather than having to open each one.
/// </summary>
public record FollowupDto
{
    public int FollowupId { get; init; }
    public int InquiryId { get; init; }
    public int StaffId { get; init; }
    public string? StaffName { get; init; }
    public DateOnly FollowupDate { get; init; }
    public string? Notes { get; init; }
    public DateOnly? NextFollowup { get; init; }
    public string? Status { get; init; }

    // Flattened from the enquiry.
    public string? EnquirerName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? CourseName { get; init; }
    public string? InquiryStatus { get; init; }

    /// <summary>How overdue the call is, in days. Negative means it is
    /// booked for the future.</summary>
    public int DaysOverdue { get; init; }
}

/// <summary>
/// Logging the outcome of a call.
///
/// The property is called Status, not Outcome, because that is what the
/// React client posts — FollowupList.jsx sends { status, notes,
/// nextFollowup }. The frontend is shared and unmodified, so its payload is
/// the contract; a better name here would simply mean the field never binds
/// and every log fails validation.
/// </summary>
public class FollowupLogRequest
{
    /// <summary>Done or No Response.</summary>
    [Required(ErrorMessage = "An outcome is required")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>Blank ends the follow-up trail rather than booking another.</summary>
    public DateOnly? NextFollowup { get; set; }
}

/// <summary>A Get in Touch message.</summary>
public record ContactMessageDto(
    int MessageId, string Name, string Email, string Message,
    bool IsRead, DateTime CreatedAt);

/// <summary>What the public Get in Touch form posts.</summary>
public class ContactMessageRequest
{
    [Required(ErrorMessage = "Name is required"), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // 500 characters, matching the BRD's "message box will allow user to
    // type a text upto 500 characters".
    [Required(ErrorMessage = "A message is required"), MaxLength(500)]
    public string Message { get; set; } = string.Empty;
}
