using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Items in the scrolling ticker at the top of the public site. Validity
/// dates work the same way banners' do — only currently-valid items are
/// served to the ticker.
/// </summary>
[Table("announcements")]
public class Announcement
{
    [Key]
    [Column("announcement_id")]
    public int AnnouncementId { get; set; }

    [Column("content")]
    [Required, MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
