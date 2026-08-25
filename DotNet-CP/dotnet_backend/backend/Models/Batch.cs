using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// A cohort of one course.
/// </summary>
[Table("batches")]
public class Batch
{
    [Key]
    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("course_id")]
    public int CourseId { get; set; }

    /// <summary>Faculty leading the batch. NOT NULL in the schema.</summary>
    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("batch_name")]
    [Required, MaxLength(100)]
    public string BatchName { get; set; } = string.Empty;

    [Column("academic_year")]
    [MaxLength(20)]
    public string? AcademicYear { get; set; }

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("presentation_date")]
    public DateTime? PresentationDate { get; set; }

    [Column("timing")]
    [MaxLength(100)]
    public string? Timing { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; } = 20;

    /// <summary>
    /// A cached figure, recomputed from enrolments on every write and never
    /// accepted from a form. A counter that drifts once stays wrong.
    /// </summary>
    [Column("current_count")]
    public int CurrentCount { get; set; }

    [Column("status")]
    public BatchStatus Status { get; set; } = BatchStatus.Upcoming;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }
}
