using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Registered students and their batch.</summary>
public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken ct = default);
    Task<StudentDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<StudentDto>> GetByBatchAsync(int batchId, CancellationToken ct = default);
}
