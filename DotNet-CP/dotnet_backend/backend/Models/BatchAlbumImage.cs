using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>A photo inside a batch album.</summary>
[Table("batch_album_images")]
public class BatchAlbumImage
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Column("album_id")]
    public int AlbumId { get; set; }

    [Column("image_url")]
    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("caption")]
    [MaxLength(255)]
    public string? Caption { get; set; }

    [Column("uploaded_by")]
    public int? UploadedBy { get; set; }

    [Column("upload_date")]
    public DateOnly UploadDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(AlbumId))]
    public BatchAlbum? Album { get; set; }
}
