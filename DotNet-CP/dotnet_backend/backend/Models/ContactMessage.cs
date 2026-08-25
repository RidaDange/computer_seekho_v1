using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// A message from the public Get in Touch form (BRD section 5).
///
/// Distinct from an enquiry: this is somebody asking a question, not a lead
/// entering the sales pipeline. No counsellor is assigned and no follow-up
/// is scheduled, and the notification email goes to the institute rather
/// than to the sender.
/// </summary>
[Table("contact_messages")]
public class ContactMessage
{
    [Key]
    [Column("message_id")]
    public int MessageId { get; set; }

    [Column("name")]
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Column("message")]
    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
