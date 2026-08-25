using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// One scheduled or logged call against an enquiry.
///
/// A row is created the moment an enquiry is captured, dated three days out.
/// An enquiry with no follow-up row would never appear on anyone's working
/// list, so scheduling is not left for someone to remember.
/// </summary>
[Table("followups")]
public class Followup
{
    [Key]
    [Column("followup_id")]
    public int FollowupId { get; set; }

    [Column("inquiry_id")]
    public int InquiryId { get; set; }

    /// <summary>Whose call it is. NOT NULL — a follow-up nobody owns is
    /// a follow-up nobody makes.</summary>
    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("followup_date")]
    public DateOnly FollowupDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>When the next call was booked for, if one was.</summary>
    [Column("next_followup")]
    public DateOnly? NextFollowup { get; set; }

    [Column("status")]
    public FollowupStatus Status { get; set; } = FollowupStatus.Pending;

    [ForeignKey(nameof(InquiryId))]
    public Inquiry? Inquiry { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }
}
