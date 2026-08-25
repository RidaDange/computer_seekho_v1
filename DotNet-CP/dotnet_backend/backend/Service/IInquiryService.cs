using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Enquiry capture and lifecycle.</summary>
public interface IInquiryService
{
    /// <param name="defaultSource">"Website" or "Walk-in", set by whichever
    /// controller action took the request.</param>
    Task<InquiryDto> CreateAsync(InquiryCreateRequest request, string defaultSource, CancellationToken ct = default);

    /// <summary>Everything, including closed and converted, for history.</summary>
    Task<IEnumerable<InquiryDto>> GetAllAsync(int? forStaffId, CancellationToken ct = default);

    /// <summary>Open enquiries only — closed ones are hidden at the query.</summary>
    Task<IEnumerable<InquiryDto>> GetActiveAsync(int? forStaffId, CancellationToken ct = default);

    Task<InquiryDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<InquiryDto?> CloseAsync(int id, CloseInquiryRequest request, CancellationToken ct = default);

    Task<InquiryDto?> ConvertAsync(int id, CancellationToken ct = default);
}
