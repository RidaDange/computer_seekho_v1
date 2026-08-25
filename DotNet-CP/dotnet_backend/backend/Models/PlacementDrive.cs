using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>A recruiter visiting to hire.</summary>
[Table("placement_drives")]
public class PlacementDrive
{
    [Key]
    [Column("drive_id")]
    public int DriveId { get; set; }

    [Column("recruiter_id")]
    public int RecruiterId { get; set; }

    [Column("course_id")]
    public int? CourseId { get; set; }

    [Column("drive_date")]
    public DateOnly DriveDate { get; set; }

    [Column("drive_mode")]
    public DriveMode DriveMode { get; set; } = DriveMode.Offline;

    [Column("position")]
    [Required, MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("eligibility_criteria")]
    public string? EligibilityCriteria { get; set; }

    /// <summary>Annual CTC in INR.</summary>
    [Column("package")]
    public decimal? Package { get; set; }

    [Column("hr_contact_name")]
    [MaxLength(150)]
    public string? HrContactName { get; set; }

    [Column("hr_contact_email")]
    [MaxLength(150)]
    public string? HrContactEmail { get; set; }

    [Column("hr_contact_phone")]
    [MaxLength(15)]
    public string? HrContactPhone { get; set; }

    [Column("no_of_openings")]
    public int? NoOfOpenings { get; set; }

    [Column("no_of_students_selected")]
    public int? NoOfStudentsSelected { get; set; }

    [Column("drive_status")]
    public DriveStatus DriveStatus { get; set; } = DriveStatus.Scheduled;

    [ForeignKey(nameof(RecruiterId))]
    public Recruiter? Recruiter { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }
}
