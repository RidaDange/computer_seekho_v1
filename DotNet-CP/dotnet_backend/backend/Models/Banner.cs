using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Home page carousel. StartDate and EndDate make a banner self-expiring —
/// the public endpoint filters on them so nobody has to remember to take an
/// out-of-date banner down.
/// </summary>
[Table("banners")]
public class Banner
{
    [Key]
    [Column("banner_id")]
    public int BannerId { get; set; }

    [Column("title")]
    [MaxLength(200)]
    public string? Title { get; set; }

    [Column("image_url")]
    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("link_url")]
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }
}
