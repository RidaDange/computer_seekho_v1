using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>Groups courses — Diploma, Certification, and so on.</summary>
[Table("course_categories")]
public class CourseCategory
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("name")]
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("age_group")]
    [MaxLength(50)]
    public string? AgeGroup { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
