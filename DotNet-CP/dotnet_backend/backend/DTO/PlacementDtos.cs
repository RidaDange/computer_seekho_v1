using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

// ---------------------------------------------------------------------------
// Placements, albums and uploads.
//
// The read DTOs use init-only properties because several members are
// resolved from other tables, and ForMember does not apply to a positional
// record's constructor parameters.
// ---------------------------------------------------------------------------

/// <summary>A recruiter visiting to hire.</summary>
public record PlacementDriveDto
{
    public int DriveId { get; init; }
    public int RecruiterId { get; init; }
    public string? RecruiterCompanyName { get; init; }
    public int? CourseId { get; init; }
    public string? CourseName { get; init; }
    public DateOnly DriveDate { get; init; }
    public string? DriveMode { get; init; }
    public string Position { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? EligibilityCriteria { get; init; }
    public decimal? Package { get; init; }
    public string? HrContactName { get; init; }
    public string? HrContactEmail { get; init; }
    public string? HrContactPhone { get; init; }
    public int? NoOfOpenings { get; init; }
    public int? NoOfStudentsSelected { get; init; }
    public string? DriveStatus { get; init; }
}

/// <summary>Create/update payload for a drive.</summary>
public class PlacementDriveRequest
{
    [Required(ErrorMessage = "A recruiter is required")]
    public int RecruiterId { get; set; }

    public int? CourseId { get; set; }

    [Required(ErrorMessage = "A drive date is required")]
    public DateOnly DriveDate { get; set; }

    public string? DriveMode { get; set; }

    [Required(ErrorMessage = "A position is required"), MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? EligibilityCriteria { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Package must be a positive amount")]
    public decimal? Package { get; set; }

    [MaxLength(150)] public string? HrContactName { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(150)]
    public string? HrContactEmail { get; set; }

    [RegularExpression(@"^$|^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
    public string? HrContactPhone { get; set; }

    [Range(0, 10000)] public int? NoOfOpenings { get; set; }
    [Range(0, 10000)] public int? NoOfStudentsSelected { get; set; }

    public string? DriveStatus { get; set; }
}

/// <summary>One student placed at one recruiter.</summary>
public record PlacementRecordDto
{
    public int PlacementId { get; init; }
    public int StudentId { get; init; }
    public string? StudentName { get; init; }
    public string? StudentPhotoUrl { get; init; }
    public int? BatchId { get; init; }
    public string? BatchName { get; init; }
    public int RecruiterId { get; init; }
    public string? RecruiterCompanyName { get; init; }
    public string? RecruiterLogoUrl { get; init; }
    public string? Position { get; init; }
    public int? DriveId { get; init; }
    public decimal? Package { get; init; }
    public DateOnly? PlacementDate { get; init; }
    public bool IsFeatured { get; init; }
}

/// <summary>Create/update payload for a placement record.</summary>
public class PlacementRecordRequest
{
    [Required(ErrorMessage = "A student is required")]
    public int StudentId { get; set; }

    public int? BatchId { get; set; }

    [Required(ErrorMessage = "A recruiter is required")]
    public int RecruiterId { get; set; }

    [MaxLength(200)] public string? Position { get; set; }
    public int? DriveId { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Package must be a positive amount")]
    public decimal? Package { get; set; }

    public DateOnly? PlacementDate { get; set; }
    public bool IsFeatured { get; set; }
}

// --- Albums ----------------------------------------------------------------

/// <summary>A photo inside a batch album.</summary>
public record BatchAlbumImageDto
{
    public int ImageId { get; init; }
    public int AlbumId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public DateOnly UploadDate { get; init; }
    public int DisplayOrder { get; init; }

    /// <summary>True for the album's cover. Computed, not a column.</summary>
    public bool IsCover { get; init; }
}

/// <summary>A batch's album with its photos.</summary>
public record BatchAlbumDto
{
    public int AlbumId { get; init; }
    public int BatchId { get; init; }
    public string? BatchName { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? CoverImageId { get; init; }
    public string? CoverImageUrl { get; init; }
    public IEnumerable<BatchAlbumImageDto> Images { get; init; } = [];
}

/// <summary>One album on the public Campus Life strip.</summary>
public record BatchAlbumSummaryDto(
    int AlbumId,
    int BatchId,
    string? BatchName,
    string Title,
    string? CoverImageUrl,
    int ImageCount);

/// <summary>Adding a photo to an album.</summary>
public class BatchAlbumImageRequest
{
    [Required(ErrorMessage = "An image is required"), MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(255)] public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}

// --- Uploads ---------------------------------------------------------------

/// <summary>
/// What the client needs after an upload: where the file now lives, and
/// enough about it to show a confirmation.
///
/// Url is relative to the API root, not absolute, so the same value works
/// from localhost and from a deployed host.
/// </summary>
public record UploadResultDto(
    string Url,
    string? OriginalFilename,
    string? ContentType,
    long SizeBytes);
