using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Places a student in a batch. Carries inquiry_id as well, so the trace
/// back to the original lead survives even if you start from the enrolment.
/// </summary>
[Table("enrollments")]
public class Enrollment
{
    [Key]
    [Column("enrollment_id")]
    public int EnrollmentId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("inquiry_id")]
    public int InquiryId { get; set; }

    [Column("enroll_date")]
    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("status")]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(BatchId))]
    public Batch? Batch { get; set; }
}
