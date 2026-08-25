using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// One photo album per batch, enforced by UNIQUE(batch_id).
///
/// CoverImageId references an image row rather than duplicating a URL, so a
/// cover cannot drift from what is actually in the album — which is exactly
/// what that foreign key exists to prevent.
/// </summary>
[Table("batch_albums")]
public class BatchAlbum
{
    [Key]
    [Column("album_id")]
    public int AlbumId { get; set; }

    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("title")]
    [Required, MaxLength(200)]
    public string Title { get; set; } = "Batch Photos";

    [Column("description")]
    public string? Description { get; set; }

    [Column("cover_image_id")]
    public int? CoverImageId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(BatchId))]
    public Batch? Batch { get; set; }
}
