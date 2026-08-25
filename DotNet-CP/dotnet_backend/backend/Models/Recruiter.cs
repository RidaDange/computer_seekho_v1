using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>Companies that hire from the academy.</summary>
[Table("recruiters")]
public class Recruiter
{
    [Key]
    [Column("recruiter_id")]
    public int RecruiterId { get; set; }

    [Column("company_name")]
    [Required, MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [Column("logo_url")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>Controls visibility on the public Our Recruiters page.</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
