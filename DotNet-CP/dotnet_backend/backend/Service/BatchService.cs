using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Service;

/// <summary>
/// Batch management.
///
/// The one rule that matters: CurrentCount is never read from the column
/// and never accepted from a form. Every response recomputes it from
/// enrolments, and every write recomputes the stored value too — a cached
/// counter that drifts once stays wrong.
/// </summary>
public class BatchService : IBatchService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<BatchService> _logger;

    public BatchService(AppDbContext db, IMapper mapper, ILogger<BatchService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<BatchDto>> GetAllAsync(CancellationToken ct = default) =>
        await WithCountsAsync(await Query().OrderByDescending(b => b.StartDate)
                                           .ThenByDescending(b => b.BatchId).ToListAsync(ct), ct);

    public async Task<IEnumerable<BatchDto>> GetCompletedAsync(CancellationToken ct = default) =>
        await WithCountsAsync(await Query()
            .Where(b => b.Status == BatchStatus.Completed && b.IsActive)
            .OrderByDescending(b => b.EndDate).ToListAsync(ct), ct);

    public async Task<BatchDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var batch = await Query().FirstOrDefaultAsync(b => b.BatchId == id, ct);
        if (batch is null) return null;

        return (await WithCountsAsync([batch], ct)).First();
    }

    public async Task<BatchDto> CreateAsync(BatchRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesExistAsync(request, ct);

        var batch = _mapper.Map<Batch>(request);
        batch.CurrentCount = 0;

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created batch '{Name}'", batch.BatchName);
        return (await GetByIdAsync(batch.BatchId, ct))!;
    }

    public async Task<BatchDto?> UpdateAsync(int id, BatchRequest request, CancellationToken ct = default)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.BatchId == id, ct);
        if (batch is null) return null;

        await EnsureReferencesExistAsync(request, ct);

        // Mapped onto the tracked instance so columns the request doesn't
        // carry survive.
        _mapper.Map(request, batch);

        // Recomputed rather than taken from the request, whatever the client
        // sent.
        batch.CurrentCount = await LiveCountAsync(batch.BatchId, ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.BatchId == id, ct);
        if (batch is null) return false;

        // Checked here so the refusal names the reason. Left to the database
        // the foreign key would reject it too, but as a constraint violation
        // that reaches the client as an opaque 500.
        if (await _db.Enrollments.AnyAsync(e => e.BatchId == id, ct))
        {
            throw new BusinessRuleException(
                $"{batch.BatchName} has students enrolled and can't be deleted.");
        }

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ------------------------------------------------------------ helpers

    private IQueryable<Batch> Query() =>
        _db.Batches.AsNoTracking()
            .Include(b => b.Course).ThenInclude(c => c!.Category)
            .Include(b => b.Staff);

    /// <summary>
    /// Counts everything except Dropped. A student who finished the course
    /// was still in the batch, so counting only Active would report every
    /// completed batch as zero enrolled.
    /// </summary>
    private async Task<int> LiveCountAsync(int batchId, CancellationToken ct) =>
        await _db.Enrollments.CountAsync(
            e => e.BatchId == batchId && e.Status != EnrollmentStatus.Dropped, ct);

    private async Task<List<BatchDto>> WithCountsAsync(List<Batch> batches, CancellationToken ct)
    {
        if (batches.Count == 0) return [];

        var ids = batches.Select(b => b.BatchId).ToList();

        var counts = await _db.Enrollments.AsNoTracking()
            .Where(e => ids.Contains(e.BatchId) && e.Status != EnrollmentStatus.Dropped)
            .GroupBy(e => e.BatchId)
            .Select(g => new { BatchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BatchId, x => x.Count, ct);

        // Placements, for the public "X/Y placed" figure. Fetched for the
        // whole set in one query rather than per batch — the alternative is
        // a count query per row.
        var placed = await _db.PlacementRecords.AsNoTracking()
            .Where(r => r.BatchId != null && ids.Contains(r.BatchId.Value))
            .GroupBy(r => r.BatchId!.Value)
            .Select(g => new { BatchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BatchId, x => x.Count, ct);

        return batches
            .Select(b => _mapper.Map<BatchDto>(b) with
            {
                CurrentCount = counts.TryGetValue(b.BatchId, out var n) ? n : 0,
                PlacedCount = placed.TryGetValue(b.BatchId, out var p) ? p : 0
            })
            .ToList();
    }

    private async Task EnsureReferencesExistAsync(BatchRequest request, CancellationToken ct)
    {
        if (!await _db.Courses.AnyAsync(c => c.CourseId == request.CourseId, ct))
        {
            throw new ResourceNotFoundException("Course", request.CourseId);
        }

        if (!await _db.Staff.AnyAsync(s => s.StaffId == request.StaffId, ct))
        {
            throw new ResourceNotFoundException("Staff", request.StaffId);
        }
    }
}
