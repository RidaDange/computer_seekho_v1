using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Home page carousel.
///
/// The public read is "currently valid" rather than merely "active",
/// because a banner carries its own start and end dates. Filtering in the
/// query rather than in the UI means an expired banner never reaches the
/// client at all, so no screen has to remember to hide one.
/// </summary>
[ApiController]
[Route("banners")]
[Authorize(Roles = "Admin,Manager")]
public class BannerController : GenericController<Banner, BannerDto, BannerRequest>
{
    public BannerController(IGenericService<Banner, BannerDto> service, ILogger<BannerController> logger)
        : base(service, logger) { }

    [HttpGet("valid")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetValid(
        [FromServices] IGenericRepository<Banner> repository,
        [FromServices] IMapper mapper,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = await repository.FindAsync(
            b => b.IsActive
                 && (b.StartDate == null || b.StartDate <= today)
                 && (b.EndDate == null || b.EndDate >= today),
            ct);

        // Ordered here rather than in the repository: display order is a
        // presentation concern and the generic repository has no business
        // knowing that banners have one.
        var ordered = rows.OrderBy(b => b.DisplayOrder).ThenBy(b => b.BannerId);

        return Ok(mapper.Map<IEnumerable<BannerDto>>(ordered));
    }
}
