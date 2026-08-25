using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// News and event items.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("news-events")]
[Authorize(Roles = "Admin,Manager")]
public class NewsEventController : GenericController<NewsEvent, NewsEventDto, NewsEventRequest>
{
    public NewsEventController(IGenericService<NewsEvent, NewsEventDto> service, ILogger<NewsEventController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only the active rows, for the public site. Anonymous: the website
    /// has to work for visitors with no account.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<NewsEventDto>>> GetActive(
        [FromServices] IGenericRepository<NewsEvent> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive, ct);
        return Ok(mapper.Map<IEnumerable<NewsEventDto>>(rows));
    }
}
