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
/// Placement records — who was placed where.
///
/// Reads are anonymous: the public Batchwise Placement and Recruiter Detail
/// pages are built entirely from these.
/// </summary>
[ApiController]
[Route("placement-records")]
[Authorize(Roles = "Admin,Manager")]
public class PlacementRecordController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public PlacementRecordController(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    private IQueryable<PlacementRecord> Query() =>
        _db.PlacementRecords.AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Batch)
            .Include(r => r.Recruiter);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<IEnumerable<PlacementRecordDto>>(
            await Query().OrderByDescending(r => r.PlacementDate)
                         .ThenByDescending(r => r.PlacementId).ToListAsync(ct)));

    [HttpGet("by-batch/{batchId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetByBatch(int batchId, CancellationToken ct) =>
        Ok(_mapper.Map<IEnumerable<PlacementRecordDto>>(
            await Query().Where(r => r.BatchId == batchId).ToListAsync(ct)));

    [HttpGet("by-recruiter/{recruiterId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetByRecruiter(int recruiterId, CancellationToken ct) =>
        Ok(_mapper.Map<IEnumerable<PlacementRecordDto>>(
            await Query().Where(r => r.RecruiterId == recruiterId).ToListAsync(ct)));

    [HttpPost]
    public async Task<ActionResult<PlacementRecordDto>> Create(PlacementRecordRequest request, CancellationToken ct)
    {
        await EnsureReferencesAsync(request, ct);

        // One student is placed at one recruiter once. A duplicate would
        // double-count them in the batch's placement percentage, which is a
        // figure shown publicly.
        if (await _db.PlacementRecords.AnyAsync(
                r => r.StudentId == request.StudentId && r.RecruiterId == request.RecruiterId, ct))
        {
            throw new BusinessRuleException("That student is already recorded as placed at this recruiter.");
        }

        var record = _mapper.Map<PlacementRecord>(request);
        _db.PlacementRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, await ReloadAsync(record.PlacementId, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacementRecordDto>> Update(int id, PlacementRecordRequest request, CancellationToken ct)
    {
        var record = await _db.PlacementRecords.FirstOrDefaultAsync(r => r.PlacementId == id, ct);
        if (record is null) return NotFound(new ApiError(404, $"Placement {id} was not found"));

        await EnsureReferencesAsync(request, ct);

        _mapper.Map(request, record);
        await _db.SaveChangesAsync(ct);

        return Ok(await ReloadAsync(id, ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var record = await _db.PlacementRecords.FirstOrDefaultAsync(r => r.PlacementId == id, ct);
        if (record is null) return NotFound(new ApiError(404, $"Placement {id} was not found"));

        _db.PlacementRecords.Remove(record);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<PlacementRecordDto> ReloadAsync(int id, CancellationToken ct) =>
        _mapper.Map<PlacementRecordDto>(await Query().FirstAsync(r => r.PlacementId == id, ct));

    private async Task EnsureReferencesAsync(PlacementRecordRequest request, CancellationToken ct)
    {
        if (!await _db.Students.AnyAsync(s => s.StudentId == request.StudentId, ct))
            throw new ResourceNotFoundException("Student", request.StudentId);

        if (!await _db.Recruiters.AnyAsync(r => r.RecruiterId == request.RecruiterId, ct))
            throw new ResourceNotFoundException("Recruiter", request.RecruiterId);
    }
}
