using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// The fixed list of reasons an enquiry may be closed with.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("closure-reasons")]
[Authorize(Roles = "Admin,Manager")]
public class ClosureReasonController : GenericController<ClosureReason, ClosureReasonDto, ClosureReasonRequest>
{
    public ClosureReasonController(IGenericService<ClosureReason, ClosureReasonDto> service, ILogger<ClosureReasonController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only the active rows, for the public site. Anonymous: the website
    /// has to work for visitors with no account.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ClosureReasonDto>>> GetActive(
        [FromServices] IGenericRepository<ClosureReason> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive, ct);
        return Ok(mapper.Map<IEnumerable<ClosureReasonDto>>(rows));
    }
}
