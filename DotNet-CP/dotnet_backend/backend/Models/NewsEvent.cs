using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>News and event items shown on the public site.</summary>
[Table("news_events")]
public class NewsEvent
{
    [Key]
    [Column("news_id")]
    public int NewsId { get; set; }

    [Column("title")]
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("image_url")]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Column("event_date")]
    public DateOnly? EventDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
