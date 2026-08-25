using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Fee collection after registration.</summary>
public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetForStudentAsync(int studentId, CancellationToken ct = default);
    Task<FeeBreakdownDto> FeesForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<PaymentDto> CollectAsync(PaymentRequest request, CancellationToken ct = default);
}
