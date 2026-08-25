using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// A lead. Everything downstream — student, enrolment, payment, placement —
/// traces back to a row here, which is why students.inquiry_id is NOT NULL.
/// </summary>
[Table("inquiries")]
public class Inquiry
{
    [Key]
    [Column("inquiry_id")]
    public int InquiryId { get; set; }

    [Column("course_id")]
    public int CourseId { get; set; }

    /// <summary>Assigned counsellor. Nullable — an enquiry can arrive when
    /// no active counsellor exists to take it.</summary>
    [Column("staff_id")]
    public int? StaffId { get; set; }

    [Column("enquirer_name")]
    [Required, MaxLength(150)]
    public string EnquirerName { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("phone")]
    [MaxLength(15)]
    public string? Phone { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("source")]
    [MaxLength(100)]
    public string? Source { get; set; }

    [Column("status")]
    public InquiryStatus Status { get; set; } = InquiryStatus.New;

    [Column("inquiry_date")]
    public DateOnly InquiryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("closure_reason_id")]
    public int? ClosureReasonId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }

    [ForeignKey(nameof(ClosureReasonId))]
    public ClosureReason? ClosureReason { get; set; }
}
