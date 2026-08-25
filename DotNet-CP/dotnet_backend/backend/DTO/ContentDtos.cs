using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

// ---------------------------------------------------------------------------
// DTOs for the master/content tables.
//
// Grouped in one file because each is a handful of lines and they are always
// read together — a file per record would be twelve files of six lines.
//
// Every one has a matching *Request with validation attributes. The pattern
// is deliberate: the read shape carries the id and any resolved display
// fields, the write shape carries neither, so a client cannot set something
// the server owns.
// ---------------------------------------------------------------------------

// --- Course category -------------------------------------------------------

public record CourseCategoryDto(
    int CategoryId, string Name, string? AgeGroup, string? Description, bool IsActive);

public class CourseCategoryRequest
{
    [Required(ErrorMessage = "Name is required"), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AgeGroup { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// --- Recruiter -------------------------------------------------------------

public record RecruiterDto(
    int RecruiterId, string CompanyName, string? LogoUrl, bool IsActive);

public class RecruiterRequest
{
    [Required(ErrorMessage = "Company name is required"), MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

// --- Closure reason --------------------------------------------------------

public record ClosureReasonDto(int ReasonId, string ReasonText, bool IsActive);

public class ClosureReasonRequest
{
    [Required(ErrorMessage = "Reason text is required"), MaxLength(200)]
    public string ReasonText { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

// --- Banner ----------------------------------------------------------------

public record BannerDto(
    int BannerId, string? Title, string ImageUrl, string? LinkUrl,
    int DisplayOrder, bool IsActive, DateOnly? StartDate, DateOnly? EndDate);

public class BannerRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "An image is required"), MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// --- Announcement ----------------------------------------------------------

public record AnnouncementDto(
    int AnnouncementId, string Content, DateOnly? StartDate, DateOnly? EndDate,
    int DisplayOrder, bool IsActive, DateTime CreatedAt);

public class AnnouncementRequest
{
    [Required(ErrorMessage = "Content is required"), MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// --- Testimonial -----------------------------------------------------------

public record TestimonialDto(
    int TestimonialId, string Name, string Content, byte? Rating,
    string? PhotoUrl, bool IsApproved, DateTime CreatedAt);

public class TestimonialRequest
{
    [Required(ErrorMessage = "Name is required"), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Mirrors the CHECK constraint on the column, so an out-of-range
    /// value is a readable message rather than a 500 from the database.</summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public byte? Rating { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public bool IsApproved { get; set; }
}

// --- News / events ---------------------------------------------------------

public record NewsEventDto(
    int NewsId, string Title, string? Content, string? ImageUrl,
    DateOnly? EventDate, bool IsActive, DateTime CreatedAt);

public class NewsEventRequest
{
    [Required(ErrorMessage = "Title is required"), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateOnly? EventDate { get; set; }
    public bool IsActive { get; set; } = true;
}

// --- Gallery ---------------------------------------------------------------

public record GalleryImageDto(
    int ImageId, string Title, string? Description, string ImageUrl,
    string? Category, DateOnly UploadDate, bool IsActive);

public class GalleryImageRequest
{
    [Required(ErrorMessage = "Title is required"), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "An image is required"), MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;
}
