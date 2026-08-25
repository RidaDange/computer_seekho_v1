using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Batches, always with a live enrolment count.</summary>
public interface IBatchService
{
    Task<IEnumerable<BatchDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<BatchDto>> GetCompletedAsync(CancellationToken ct = default);
    Task<BatchDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BatchDto> CreateAsync(BatchRequest request, CancellationToken ct = default);
    Task<BatchDto?> UpdateAsync(int id, BatchRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
