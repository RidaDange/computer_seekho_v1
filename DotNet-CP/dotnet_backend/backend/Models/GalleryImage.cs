using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Campus Life gallery. Category groups images into themes and is a plain
/// string rather than a foreign key — the set is small, editorial and
/// changes with the site rather than with the business.
/// </summary>
[Table("gallery_images")]
public class GalleryImage
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Column("title")]
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("category")]
    [MaxLength(100)]
    public string? Category { get; set; }

    [Column("upload_date")]
    public DateOnly UploadDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
