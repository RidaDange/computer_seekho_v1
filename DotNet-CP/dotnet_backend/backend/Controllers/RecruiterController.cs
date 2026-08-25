using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Companies that hire from the academy. Named in BRD 6.4 as an example master table.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("recruiters")]
[Authorize(Roles = "Admin,Manager")]
public class RecruiterController : GenericController<Recruiter, RecruiterDto, RecruiterRequest>
{
    public RecruiterController(IGenericService<Recruiter, RecruiterDto> service, ILogger<RecruiterController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only the active rows, for the public site. Anonymous: the website
    /// has to work for visitors with no account.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RecruiterDto>>> GetActive(
        [FromServices] IGenericRepository<Recruiter> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive, ct);
        return Ok(mapper.Map<IEnumerable<RecruiterDto>>(rows));
    }
}
