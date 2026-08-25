using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>One student placed at one recruiter.</summary>
[Table("placement_records")]
public class PlacementRecord
{
    [Key]
    [Column("placement_id")]
    public int PlacementId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("batch_id")]
    public int? BatchId { get; set; }

    [Column("recruiter_id")]
    public int RecruiterId { get; set; }

    [Column("position")]
    [MaxLength(200)]
    public string? Position { get; set; }

    [Column("drive_id")]
    public int? DriveId { get; set; }

    /// <summary>Annual CTC in INR.</summary>
    [Column("package")]
    public decimal? Package { get; set; }

    [Column("placement_date")]
    public DateOnly? PlacementDate { get; set; }

    /// <summary>
    /// Saved but currently read by nothing on the public site — the same gap
    /// the Java backend has. Kept so the column and the admin checkbox stay
    /// in step rather than silently diverging.
    /// </summary>
    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(BatchId))]
    public Batch? Batch { get; set; }

    [ForeignKey(nameof(RecruiterId))]
    public Recruiter? Recruiter { get; set; }
}
