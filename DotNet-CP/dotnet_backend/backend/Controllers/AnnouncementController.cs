using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Ticker items on the public site. Same validity-window treatment as
/// banners — each item expires on its own rather than needing someone to
/// remember to remove it.
/// </summary>
[ApiController]
[Route("announcements")]
[Authorize(Roles = "Admin,Manager")]
public class AnnouncementController : GenericController<Announcement, AnnouncementDto, AnnouncementRequest>
{
    public AnnouncementController(
        IGenericService<Announcement, AnnouncementDto> service,
        ILogger<AnnouncementController> logger)
        : base(service, logger) { }

    [HttpGet("valid")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AnnouncementDto>>> GetValid(
        [FromServices] IGenericRepository<Announcement> repository,
        [FromServices] IMapper mapper,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = await repository.FindAsync(
            a => a.IsActive
                 && (a.StartDate == null || a.StartDate <= today)
                 && (a.EndDate == null || a.EndDate >= today),
            ct);

        var ordered = rows.OrderBy(a => a.DisplayOrder).ThenBy(a => a.AnnouncementId);

        return Ok(mapper.Map<IEnumerable<AnnouncementDto>>(ordered));
    }
}
