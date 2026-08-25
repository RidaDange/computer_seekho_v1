using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// What the academy sells. `Fees` drives the entire payment chain — the
/// installment split, the receipt and the outstanding balance all derive
/// from this one figure.
/// </summary>
[Table("courses")]
public class Course
{
    [Key]
    [Column("course_id")]
    public int CourseId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("name")]
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("duration")]
    [MaxLength(50)]
    public string? Duration { get; set; }

    [Column("fees")]
    public decimal Fees { get; set; }

    [Column("level")]
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    [Column("syllabus_url")]
    [MaxLength(500)]
    public string? SyllabusUrl { get; set; }

    [Column("cover_photo")]
    [MaxLength(255)]
    public string? CoverPhoto { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(CategoryId))]
    public CourseCategory? Category { get; set; }

    public ICollection<CourseStaff> CourseStaff { get; set; } = new List<CourseStaff>();
}
