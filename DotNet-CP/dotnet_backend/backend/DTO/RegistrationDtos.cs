using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

// ---------------------------------------------------------------------------
// Registration, students, batches and payments.
//
// Property names match the Java DTOs exactly — the React screens read
// result.student.batchName, fees.installment1Amount and so on directly.
// ---------------------------------------------------------------------------

/// <summary>A student, with their current enrolment flattened on.</summary>
public record StudentDto
{
    public int StudentId { get; init; }
    public int InquiryId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ParentName { get; init; } = string.Empty;
    public string? ParentPhone { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateOnly? Dob { get; init; }
    public string? Gender { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Pincode { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Qualification { get; init; }
    public DateOnly RegDate { get; init; }

    // Resolved from the enrolment, not columns on students.
    public int? EnrollmentId { get; init; }
    public int? BatchId { get; init; }
    public string? BatchName { get; init; }
    public string? CourseName { get; init; }
}

/// <summary>Step 2 of the registration wizard.</summary>
public class StudentDetailsRequest
{
    [Required(ErrorMessage = "First name is required"), MaxLength(150)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required"), MaxLength(150)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parent/guardian name is required"), MaxLength(150)]
    public string ParentName { get; set; } = string.Empty;

    [RegularExpression(@"^$|^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
    public string? ParentPhone { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
    public string Phone { get; set; } = string.Empty;

    public DateOnly? Dob { get; set; }
    public string? Gender { get; set; }

    [MaxLength(255)] public string? AddressLine1 { get; set; }
    [MaxLength(255)] public string? AddressLine2 { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? State { get; set; }

    [RegularExpression(@"^$|^\d{6}$", ErrorMessage = "Enter a valid 6-digit pincode")]
    public string? Pincode { get; set; }

    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [MaxLength(150)] public string? Qualification { get; set; }
}

/// <summary>
/// The whole wizard in one payload, saved as one transaction.
///
/// Deliberately not three endpoints: a student with no enrolment, or an
/// enrolment with no payment, is a half-registered record nothing
/// downstream can use and nobody would notice.
/// </summary>
public class RegistrationRequest
{
    [Required(ErrorMessage = "An enquiry is required — registration can't happen without one")]
    public int InquiryId { get; set; }

    /// <summary>
    /// The course actually being registered for. Null falls back to the
    /// enquiry's own. BRD section 6.1 anticipates enquiring about course A
    /// and registering for course B.
    /// </summary>
    public int? CourseId { get; set; }

    [Required(ErrorMessage = "Select a batch")]
    public int BatchId { get; set; }

    [Required(ErrorMessage = "Student details are required")]
    public StudentDetailsRequest Student { get; set; } = new();

    /// <summary>Null defaults to installment 1 of the course fee.</summary>
    public decimal? AmountPaid { get; set; }

    public string? PaymentMode { get; set; }
    public string? TransactionId { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>What a course costs and when.</summary>
public record FeeBreakdownDto(
    int CourseId,
    string CourseName,
    decimal TotalFees,
    int TotalInstallments,
    decimal Installment1Amount,
    DateOnly Installment1DueDate,
    decimal Installment2Amount,
    DateOnly Installment2DueDate);

/// <summary>
/// A recorded payment.
///
/// Init-only properties, not a positional record — and the distinction is
/// load-bearing whenever a mapping needs ForMember.
///
/// AutoMapper maps a positional record through its constructor, and
/// ForMember does not apply to constructor parameters. StudentName has no
/// counterpart on Payment (it comes from the related student), so mapping
/// would fail outright. PaymentMode would be worse: it would resolve by
/// name from the enum and silently produce "BankTransfer" where the
/// database and the UI both say "Bank Transfer".
///
/// Rule of thumb for this project: a DTO that needs any ForMember must use
/// init-only properties. The content DTOs stay positional because every one
/// of their members matches by name.
/// </summary>
public record PaymentDto
{
    public int PaymentId { get; init; }
    public int StudentId { get; init; }

    /// <summary>Resolved from the related student, not a column on payments.</summary>
    public string? StudentName { get; init; }

    public int EnrollmentId { get; init; }
    public decimal Amount { get; init; }
    public int InstallmentNumber { get; init; }
    public int TotalInstallments { get; init; }
    public DateOnly PaymentDate { get; init; }

    /// <summary>The database spelling, including "Bank Transfer" with its space.</summary>
    public string? PaymentMode { get; init; }

    public string? PaymentStatus { get; init; }
    public string? TransactionId { get; init; }
    public string ReceiptNo { get; init; } = string.Empty;
    public string? Remarks { get; init; }
}

/// <summary>Collecting installment two.</summary>
public class PaymentRequest
{
    [Required(ErrorMessage = "An enrolment is required")]
    public int EnrollmentId { get; set; }

    [Required(ErrorMessage = "An amount is required")]
    [Range(1, 10_000_000, ErrorMessage = "Enter a positive amount")]
    public decimal Amount { get; set; }

    public string? PaymentMode { get; set; }
    public string? TransactionId { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Everything the confirmation screen needs after registering.</summary>
public record RegistrationResult(
    StudentDto Student,
    PaymentDto FirstPayment,
    FeeBreakdownDto FeeBreakdown,
    string ReceiptDownloadPath,
    bool ReceiptEmailed);

/// <summary>A batch, with its live enrolment count.</summary>
public record BatchDto
{
    public int BatchId { get; init; }
    public string BatchName { get; init; } = string.Empty;
    public int CourseId { get; init; }
    public string? CourseName { get; init; }
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int StaffId { get; init; }
    public string? StaffName { get; init; }
    public string? AcademicYear { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Timing { get; init; }
    public int Capacity { get; init; }

    /// <summary>Recomputed from enrolments, never read from the column.</summary>
    public int CurrentCount { get; init; }

    /// <summary>
    /// How many of the batch's students have a placement record.
    ///
    /// Resolved from placement_records, not a column on batches. The public
    /// Batchwise Placement page divides this by Capacity to show
    /// "X/Y placed" — so omitting it doesn't produce an error, it produces
    /// NaN% on a public page, which is worse.
    ///
    /// Note it divides by Capacity rather than CurrentCount, matching the
    /// Java behaviour: the headline figure is against the seats the batch
    /// was sold with, not against however many turned up.
    /// </summary>
    public int PlacedCount { get; init; }

    public string? Status { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>Create/update payload for a batch.</summary>
public class BatchRequest
{
    [Required(ErrorMessage = "A batch name is required"), MaxLength(100)]
    public string BatchName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A course is required")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "A faculty member is required")]
    public int StaffId { get; set; }

    [MaxLength(20)] public string? AcademicYear { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime? PresentationDate { get; set; }
    [MaxLength(100)] public string? Timing { get; set; }

    [Range(1, 500, ErrorMessage = "Capacity must be between 1 and 500")]
    public int Capacity { get; set; } = 20;

    public string? Status { get; set; }
    public bool IsActive { get; set; } = true;
}
