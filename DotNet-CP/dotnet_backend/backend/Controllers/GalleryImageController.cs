using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Campus Life gallery.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("gallery-images")]
[Authorize(Roles = "Admin,Manager")]
public class GalleryImageController : GenericController<GalleryImage, GalleryImageDto, GalleryImageRequest>
{
    public GalleryImageController(IGenericService<GalleryImage, GalleryImageDto> service, ILogger<GalleryImageController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only the active rows, for the public site. Anonymous: the website
    /// has to work for visitors with no account.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GalleryImageDto>>> GetActive(
        [FromServices] IGenericRepository<GalleryImage> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive, ct);
        return Ok(mapper.Map<IEnumerable<GalleryImageDto>>(rows));
    }

    /// <summary>
    /// The distinct categories, with a cover image for each — what the
    /// Campus Life page shows as a grid of themes.
    ///
    /// Grouped in memory rather than with a GROUP BY, because the gallery is
    /// editorial content numbering dozens of rows, not thousands. Pushing it
    /// into SQL would mean a bespoke query on a repository that deliberately
    /// knows nothing about gallery images.
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GalleryCategoryDto>>> GetCategories(
        [FromServices] IGenericRepository<GalleryImage> repository,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive && e.Category != null, ct);

        var categories = rows
            .GroupBy(i => i.Category!)
            .Select(g => new GalleryCategoryDto(
                g.Key,
                g.Count(),
                g.OrderBy(i => i.ImageId).First().ImageUrl))
            .OrderBy(c => c.Category)
            .ToList();

        return Ok(categories);
    }

    /// <summary>Every image in one category.</summary>
    [HttpGet("by-category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GalleryImageDto>>> GetByCategory(
        string category,
        [FromServices] IGenericRepository<GalleryImage> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(
            e => e.IsActive && e.Category != null && e.Category.ToLower() == category.ToLower(), ct);

        return Ok(mapper.Map<IEnumerable<GalleryImageDto>>(rows.OrderBy(i => i.ImageId)));
    }
}
