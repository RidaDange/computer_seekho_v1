using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Student quotes. IsApproved defaults to false, so nothing a student
/// submits reaches the public site without a staff member allowing it.
/// </summary>
[Table("testimonials")]
public class Testimonial
{
    [Key]
    [Column("testimonial_id")]
    public int TestimonialId { get; set; }

    [Column("name")]
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("content")]
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>1 to 5. The database has a CHECK constraint; the DTO
    /// repeats it so a bad value is a readable message rather than a
    /// constraint violation surfacing as a 500.</summary>
    [Column("rating")]
    public byte? Rating { get; set; }

    [Column("photo_url")]
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("is_approved")]
    public bool IsApproved { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
