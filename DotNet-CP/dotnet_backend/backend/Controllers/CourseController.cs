using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Courses.
///
/// Not purely generic: a CourseDto carries its category name, which lives on
/// another table. The generic service maps from an entity loaded without its
/// relationships, so CategoryName would come back null. The two reads below
/// therefore go through the repository with the category included, and map
/// explicitly.
///
/// That is the honest boundary of the generic approach — it covers CRUD, and
/// stops covering it the moment a response needs data from a second table.
/// </summary>
[ApiController]
[Route("courses")]
[Authorize(Roles = "Admin,Manager")]
public class CourseController : GenericController<Course, CourseDto, CourseCreateRequest>
{
    private readonly IGenericRepository<Course> _repository;
    private readonly IMapper _mapper;

    public CourseController(
        IGenericService<Course, CourseDto> service,
        IGenericRepository<Course> repository,
        IMapper mapper,
        ILogger<CourseController> logger)
        : base(service, logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>The relationships every course read needs loaded.</summary>
    private static readonly System.Linq.Expressions.Expression<Func<Course, object>>[] Includes =
        new System.Linq.Expressions.Expression<Func<Course, object>>[] { c => c.Category! };

    /// <summary>
    /// Active courses, for the public site's course list and the enquiry
    /// form's dropdown.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetActive(CancellationToken ct)
    {
        var rows = await _repository.FindWithIncludesAsync(c => c.IsActive, Includes, ct);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(rows));
    }

    /// <summary>
    /// Sets the course's lead faculty.
    ///
    /// Writes to course_staff, not to a column on Course, which is why this
    /// is its own endpoint rather than a field on the edit form.
    ///
    /// The subtlety: course_staff has UNIQUE(course_id, staff_id), so
    /// promoting someone who ALREADY teaches the course must update their
    /// existing row. Inserting a second one violates the constraint — and
    /// that is precisely the case that occurs most often, since the lead is
    /// usually already on the team.
    /// </summary>
    [HttpPut("{id:int}/primary-faculty/{staffId:int}")]
    public async Task<ActionResult<CourseDto>> SetPrimaryFaculty(
        int id, int staffId,
        [FromServices] Models.AppDbContext db,
        CancellationToken ct)
    {
        if (!await db.Courses.AnyAsync(c => c.CourseId == id, ct))
        {
            return NotFoundError(id);
        }

        var staff = await db.Staff.FirstOrDefaultAsync(s => s.StaffId == staffId, ct)
            ?? throw new ResourceNotFoundException("Staff", staffId);

        if (staff.Role != Models.StaffRole.Faculty || !staff.IsActive)
        {
            throw new BusinessRuleException(
                $"{staff.Name} is not an active faculty member and can't lead a course.");
        }

        // Demote whoever currently holds it.
        var existingPrimary = await db.CourseStaff
            .Where(cs => cs.CourseId == id && cs.IsPrimary)
            .ToListAsync(ct);

        foreach (var cs in existingPrimary) cs.IsPrimary = false;

        // Reuse the row if they already teach this course; insert only if
        // they don't.
        var link = await db.CourseStaff
            .FirstOrDefaultAsync(cs => cs.CourseId == id && cs.StaffId == staffId, ct);

        if (link is null)
        {
            db.CourseStaff.Add(new Models.CourseStaff
            {
                CourseId = id,
                StaffId = staffId,
                IsPrimary = true,
                AssignedDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }
        else
        {
            link.IsPrimary = true;
        }

        await db.SaveChangesAsync(ct);

        return await GetById(id, ct);
    }

    /// <summary>
    /// Overridden so the category is loaded. The inherited version maps a
    /// Course without its relationships, which would leave CategoryName
    /// null on the Courses maintenance grid.
    /// </summary>
    [AllowAnonymous]
    public override async Task<ActionResult<IEnumerable<CourseDto>>> GetAll(CancellationToken ct)
    {
        var rows = await _repository.FindWithIncludesAsync(null, Includes, ct);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(rows));
    }

    /// <summary>Overridden for the same reason as GetAll.</summary>
    [AllowAnonymous]
    public override async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken ct)
    {
        var course = await _repository.FirstWithIncludesAsync(c => c.CourseId == id, Includes, ct);
        return course is null ? NotFoundError(id) : Ok(_mapper.Map<CourseDto>(course));
    }
}
