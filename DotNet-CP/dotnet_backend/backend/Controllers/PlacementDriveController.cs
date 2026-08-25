using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Controllers;

/// <summary>Recruiter visits.</summary>
[ApiController]
[Route("placement-drives")]
[Authorize(Roles = "Admin,Manager")]
public class PlacementDriveController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public PlacementDriveController(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    private IQueryable<PlacementDrive> Query() =>
        _db.PlacementDrives.AsNoTracking()
            .Include(d => d.Recruiter)
            .Include(d => d.Course);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlacementDriveDto>>> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<IEnumerable<PlacementDriveDto>>(
            await Query().OrderByDescending(d => d.DriveDate)
                         .ThenByDescending(d => d.DriveId).ToListAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlacementDriveDto>> GetById(int id, CancellationToken ct)
    {
        var drive = await Query().FirstOrDefaultAsync(d => d.DriveId == id, ct);
        return drive is null
            ? NotFound(new ApiError(404, $"Drive {id} was not found"))
            : Ok(_mapper.Map<PlacementDriveDto>(drive));
    }

    [HttpPost]
    public async Task<ActionResult<PlacementDriveDto>> Create(PlacementDriveRequest request, CancellationToken ct)
    {
        if (!await _db.Recruiters.AnyAsync(r => r.RecruiterId == request.RecruiterId, ct))
            throw new ResourceNotFoundException("Recruiter", request.RecruiterId);

        var drive = _mapper.Map<PlacementDrive>(request);
        _db.PlacementDrives.Add(drive);
        await _db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created,
            _mapper.Map<PlacementDriveDto>(await Query().FirstAsync(d => d.DriveId == drive.DriveId, ct)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacementDriveDto>> Update(int id, PlacementDriveRequest request, CancellationToken ct)
    {
        var drive = await _db.PlacementDrives.FirstOrDefaultAsync(d => d.DriveId == id, ct);
        if (drive is null) return NotFound(new ApiError(404, $"Drive {id} was not found"));

        _mapper.Map(request, drive);
        await _db.SaveChangesAsync(ct);

        return Ok(_mapper.Map<PlacementDriveDto>(await Query().FirstAsync(d => d.DriveId == id, ct)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var drive = await _db.PlacementDrives.FirstOrDefaultAsync(d => d.DriveId == id, ct);
        if (drive is null) return NotFound(new ApiError(404, $"Drive {id} was not found"));

        _db.PlacementDrives.Remove(drive);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
