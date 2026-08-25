using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Get in Touch messages (BRD section 5).</summary>
public interface IContactMessageService
{
    Task<ContactMessageDto> SubmitAsync(ContactMessageRequest request, CancellationToken ct = default);
    Task<IEnumerable<ContactMessageDto>> GetAllAsync(CancellationToken ct = default);
    Task<bool> MarkReadAsync(int id, CancellationToken ct = default);
}
