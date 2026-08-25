using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Service;

/// <summary>Collecting installment two.</summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly FeeCalculator _fees;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, FeeCalculator fees, IMapper mapper, ILogger<PaymentService> logger)
    {
        _db = db;
        _fees = fees;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<PaymentDto>> GetForStudentAsync(int studentId, CancellationToken ct = default)
    {
        var rows = await _db.Payments.AsNoTracking()
            .Include(p => p.Student)
            .Where(p => p.StudentId == studentId)
            .OrderBy(p => p.InstallmentNumber)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<PaymentDto>>(rows);
    }

    public async Task<FeeBreakdownDto> FeesForEnrollmentAsync(int enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Batch).ThenInclude(b => b!.Course)
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId, ct)
            ?? throw new ResourceNotFoundException("Enrolment", enrollmentId);

        var course = enrollment.Batch?.Course
            ?? throw new BusinessRuleException("That enrolment's batch has no course.");

        // Dated from the enrolment, not from today — the due date on
        // installment two was fixed when the student registered and must not
        // move every time somebody opens the screen.
        return _fees.BreakdownFor(course, enrollment.EnrollDate);
    }

    public async Task<PaymentDto> CollectAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Batch).ThenInclude(b => b!.Course)
            .FirstOrDefaultAsync(e => e.EnrollmentId == request.EnrollmentId, ct)
            ?? throw new ResourceNotFoundException("Enrolment", request.EnrollmentId);

        var course = enrollment.Batch?.Course
            ?? throw new BusinessRuleException("That enrolment's batch has no course.");

        var fees = _fees.BreakdownFor(course, enrollment.EnrollDate);

        var paid = await _db.Payments
            .Where(p => p.EnrollmentId == enrollment.EnrollmentId && p.PaymentStatus == PaymentStatus.Success)
            .SumAsync(p => p.Amount, ct);

        var outstanding = fees.TotalFees - paid;

        if (outstanding <= 0)
        {
            throw new BusinessRuleException("This enrolment is already paid in full.");
        }

        if (request.Amount > outstanding)
        {
            throw new BusinessRuleException(
                $"That's more than the {outstanding} still outstanding.");
        }

        // Installment number derived from what already exists rather than
        // sent by the client — otherwise two collections could both claim to
        // be number 2 and the receipt numbers would collide.
        var nextInstallment = await _db.Payments
            .Where(p => p.EnrollmentId == enrollment.EnrollmentId)
            .CountAsync(ct) + 1;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var payment = new Payment
        {
            StudentId = enrollment.StudentId,
            EnrollmentId = enrollment.EnrollmentId,
            Amount = request.Amount,
            InstallmentNumber = nextInstallment,
            TotalInstallments = FeeCalculator.TotalInstallments,
            PaymentDate = today,
            PaymentMode = RegistrationService.ParseMode(request.PaymentMode),
            PaymentStatus = PaymentStatus.Success,
            TransactionId = request.TransactionId,
            Remarks = request.Remarks,
            ReceiptNo = _fees.ReceiptNumberFor(enrollment.StudentId, nextInstallment, today)
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Collected {Amount} as installment {Number} for student {StudentId}",
            payment.Amount, payment.InstallmentNumber, payment.StudentId);

        return _mapper.Map<PaymentDto>(payment);
    }
}
