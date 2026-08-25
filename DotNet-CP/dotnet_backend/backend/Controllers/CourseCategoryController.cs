using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Course groupings — Diploma, Certification and so on.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("course-categories")]
[Authorize(Roles = "Admin,Manager")]
public class CourseCategoryController : GenericController<CourseCategory, CourseCategoryDto, CourseCategoryRequest>
{
    public CourseCategoryController(IGenericService<CourseCategory, CourseCategoryDto> service, ILogger<CourseCategoryController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only the active rows, for the public site. Anonymous: the website
    /// has to work for visitors with no account.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CourseCategoryDto>>> GetActive(
        [FromServices] IGenericRepository<CourseCategory> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsActive, ct);
        return Ok(mapper.Map<IEnumerable<CourseCategoryDto>>(rows));
    }
}
