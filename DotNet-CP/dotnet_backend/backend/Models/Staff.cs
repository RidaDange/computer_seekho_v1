using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// A member of staff — the "Employee" of requirement 9.
///
/// Maps to the existing `staff` table, which the Java backend also reads and
/// writes. Column names are given explicitly because MySQL uses snake_case
/// and C# uses PascalCase; without [Column] EF Core would look for a column
/// called "StaffId" and fail at the first query.
/// </summary>
[Table("staff")]
public class Staff
{
    [Key]
    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("name")]
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(15)]
    public string? Phone { get; set; }

    [Column("role")]
    public StaffRole Role { get; set; } = StaffRole.Counselor;

    [Column("qualification")]
    [MaxLength(200)]
    public string? Qualification { get; set; }

    [Column("experience")]
    public decimal? Experience { get; set; }

    [Column("photo_url")]
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("username")]
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// A BCrypt hash written by Java's BCryptPasswordEncoder. BCrypt is a
    /// portable format, so .NET verifies these directly — no password reset
    /// is needed when switching backends.
    ///
    /// Never leaves the server: StaffDto has no corresponding field, which
    /// is the reason DTOs exist at all.
    /// </summary>
    [Column("password_hash")]
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<CourseStaff> CourseStaff { get; set; } = new List<CourseStaff>();
    public ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
}
