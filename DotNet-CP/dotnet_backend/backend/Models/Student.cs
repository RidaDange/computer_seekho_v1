using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// A registered student.
///
/// InquiryId is NOT NULL, which is the database half of BRD section 4.1 —
/// "no enquiry, no registration". Every student is traceable to where they
/// came from, and the schema refuses to hold one that isn't.
/// </summary>
[Table("students")]
public class Student
{
    [Key]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("inquiry_id")]
    public int InquiryId { get; set; }

    [Column("first_name")]
    [Required, MaxLength(150)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    [Required, MaxLength(150)]
    public string LastName { get; set; } = string.Empty;

    [Column("parent_name")]
    [Required, MaxLength(150)]
    public string ParentName { get; set; } = string.Empty;

    [Column("parent_phone")]
    [MaxLength(15)]
    public string? ParentPhone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("phone")]
    [MaxLength(15)]
    public string? Phone { get; set; }

    [Column("dob")]
    public DateOnly? Dob { get; set; }

    [Column("gender")]
    public Gender? Gender { get; set; }

    [Column("address_line1")]
    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [Column("address_line2")]
    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(100)]
    public string? State { get; set; }

    [Column("pincode")]
    [MaxLength(10)]
    public string? Pincode { get; set; }

    [Column("photo_url")]
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("qualification")]
    [MaxLength(150)]
    public string? Qualification { get; set; }

    [Column("reg_date")]
    public DateOnly RegDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [ForeignKey(nameof(InquiryId))]
    public Inquiry? Inquiry { get; set; }
}
