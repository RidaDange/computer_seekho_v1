using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Student quotes. Only approved ones reach the public site.
///
/// Everything ordinary comes from GenericController. Only the route, the
/// authorisation and the one public read below are declared here.
/// </summary>
[ApiController]
[Route("testimonials")]
[Authorize(Roles = "Admin,Manager")]
public class TestimonialController : GenericController<Testimonial, TestimonialDto, TestimonialRequest>
{
    public TestimonialController(IGenericService<Testimonial, TestimonialDto> service, ILogger<TestimonialController> logger)
        : base(service, logger) { }

    /// <summary>
    /// Only approved testimonials. Nothing a student submits appears on the
    /// public site until a staff member has allowed it.
    /// </summary>
    [HttpGet("approved")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TestimonialDto>>> GetApproved(
        [FromServices] IGenericRepository<Testimonial> repository,
        [FromServices] AutoMapper.IMapper mapper,
        CancellationToken ct)
    {
        var rows = await repository.FindAsync(e => e.IsApproved, ct);
        return Ok(mapper.Map<IEnumerable<TestimonialDto>>(rows));
    }
}
