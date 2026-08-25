using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

/// <summary>
/// A course as the API exposes it.
///
/// Declared with init-only properties rather than as a positional record,
/// unlike the other DTOs here — and the difference is deliberate.
///
/// AutoMapper maps a positional record through its constructor, and every
/// constructor parameter must be satisfiable. Three of these have no
/// counterpart on the Course entity: CategoryName lives on another table,
/// and the primary-faculty fields are resolved from course_staff. On a
/// positional record, ForMember(...).Ignore() does not apply to constructor
/// parameters, so mapping fails at runtime with "Error mapping types" and
/// the real reason buried two exceptions down.
///
/// Init-only properties make AutoMapper use property mapping instead, where
/// Ignore and MapFrom behave as expected. The type is still a record and
/// still immutable once constructed.
/// </summary>
public record CourseDto
{
    public int CourseId { get; init; }
    public int CategoryId { get; init; }

    /// <summary>Resolved from the related category; null when it wasn't loaded.</summary>
    public string? CategoryName { get; init; }

    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Duration { get; init; }
    public decimal Fees { get; init; }
    public string? Level { get; init; }
    public string? SyllabusUrl { get; init; }
    public string? CoverPhoto { get; init; }
    public bool IsActive { get; init; }

    /// <summary>
    /// Resolved from course_staff where is_primary is set. Not part of the
    /// Course row, so it is populated by the service rather than mapped.
    /// </summary>
    public int? PrimaryFacultyId { get; init; }
    public string? PrimaryFacultyName { get; init; }
}

/// <summary>Create/update payload for a course.</summary>
public class CourseCreateRequest
{
    [Required(ErrorMessage = "Category is required")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Course name is required")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Duration { get; set; }

    [Required(ErrorMessage = "Fees are required")]
    [Range(0, 10_000_000, ErrorMessage = "Fees must be a positive amount")]
    public decimal Fees { get; set; }

    public string? Level { get; set; }

    [MaxLength(500)]
    public string? SyllabusUrl { get; set; }

    [MaxLength(255)]
    public string? CoverPhoto { get; set; }

    public bool IsActive { get; set; } = true;
}
