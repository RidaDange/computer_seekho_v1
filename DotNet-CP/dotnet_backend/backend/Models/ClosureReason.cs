using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// The fixed list of reasons an enquiry may be closed with. Free text is
/// deliberately impossible — a closed enquiry drops off the follow-up list,
/// so this is the only record of why it ended.
/// </summary>
[Table("closure_reasons")]
public class ClosureReason
{
    [Key]
    [Column("reason_id")]
    public int ReasonId { get; set; }

    [Column("reason_text")]
    [Required, MaxLength(200)]
    public string ReasonText { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
