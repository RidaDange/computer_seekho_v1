namespace ComputerSeekho.DTO;

/// <summary>
/// One theme on the Campus Life page: its name, how many photos it holds,
/// and one of them to show as the cover.
///
/// Not a table — it is computed from gallery_images, which is why it has no
/// entity and no AutoMapper profile.
/// </summary>
public record GalleryCategoryDto(
    string Category,
    int ImageCount,
    string? CoverImageUrl);
