using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Service;

/// <summary>
/// Students, each with their current enrolment attached.
///
/// The enrolment is fetched for the whole set in one query rather than per
/// student — the alternative is a query per row, which is the N+1 problem
/// the Java side avoids with a fetch join.
/// </summary>
public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public StudentService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var students = await _db.Students.AsNoTracking()
            .OrderByDescending(s => s.StudentId)
            .ToListAsync(ct);

        return await AttachEnrollmentsAsync(students, ct);
    }

    public async Task<StudentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id, ct);
        if (student is null) return null;

        return (await AttachEnrollmentsAsync([student], ct)).FirstOrDefault();
    }

    public async Task<IEnumerable<StudentDto>> GetByBatchAsync(int batchId, CancellationToken ct = default)
    {
        var studentIds = await _db.Enrollments.AsNoTracking()
            .Where(e => e.BatchId == batchId && e.Status != EnrollmentStatus.Dropped)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        var students = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.StudentId))
            .OrderBy(s => s.StudentId)
            .ToListAsync(ct);

        return await AttachEnrollmentsAsync(students, ct);
    }

    private async Task<List<StudentDto>> AttachEnrollmentsAsync(List<Student> students, CancellationToken ct)
    {
        if (students.Count == 0) return [];

        var ids = students.Select(s => s.StudentId).ToList();

        var enrollments = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Batch).ThenInclude(b => b!.Course)
            .Where(e => ids.Contains(e.StudentId))
            .ToListAsync(ct);

        // The most recent enrolment is the current one. A student who moved
        // batches has more than one, and the old one is history.
        var byStudent = enrollments
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.EnrollmentId).First());

        return students.Select(s =>
        {
            var dto = _mapper.Map<StudentDto>(s);
            if (!byStudent.TryGetValue(s.StudentId, out var e)) return dto;

            return dto with
            {
                EnrollmentId = e.EnrollmentId,
                BatchId = e.BatchId,
                BatchName = e.Batch?.BatchName,
                CourseName = e.Batch?.Course?.Name
            };
        }).ToList();
    }
}
