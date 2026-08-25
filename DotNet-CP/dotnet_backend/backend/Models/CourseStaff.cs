using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerSeekho.Models;

/// <summary>
/// Junction between courses and the staff who teach them.
///
/// IsPrimary marks the lead faculty. The table has a UNIQUE(course_id,
/// staff_id), so promoting someone who already teaches the course has to
/// UPDATE their existing row — inserting a second one violates the
/// constraint. That rule lives in the service layer, not here.
/// </summary>
[Table("course_staff")]
public class CourseStaff
{
    [Key]
    [Column("course_staff_id")]
    public int CourseStaffId { get; set; }

    [Column("course_id")]
    public int CourseId { get; set; }

    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("assigned_date")]
    public DateOnly? AssignedDate { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }
}
