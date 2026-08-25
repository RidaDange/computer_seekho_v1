using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComputerSeekho.Service;

/// <summary>
/// Student registration — the transactional core of the system.
///
/// Everything happens in ONE transaction: student, enrolment, first
/// payment, the batch headcount and the enquiry's status. Split across
/// three calls you could end up with a student who has no enrolment, or an
/// enrolment nobody paid for — records nothing downstream can use, with no
/// natural moment at which anyone would notice.
///
/// This service talks to the DbContext directly rather than through
/// IGenericRepository, and that is deliberate: the generic repository calls
/// SaveChanges on every write, which is exactly what a multi-table
/// transaction must not do. The repository is the right tool for CRUD and
/// the wrong one here.
/// </summary>
public class RegistrationService : IRegistrationService
{
    /// <summary>Batches you can still join. Completed or Cancelled cannot take students.</summary>
    private static readonly BatchStatus[] JoinableStatuses =
        new[] { BatchStatus.Upcoming, BatchStatus.Ongoing };

    private readonly AppDbContext _db;
    private readonly FeeCalculator _fees;
    private readonly IEmailService _emailService;
    private readonly JavaMicroserviceClient _javaClient;
    private readonly IMapper _mapper;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        AppDbContext db,
        FeeCalculator fees,
        IEmailService emailService,
        JavaMicroserviceClient javaClient,
        IMapper mapper,
        ILogger<RegistrationService> logger)
    {
        _db = db;
        _fees = fees;
        _emailService = emailService;
        _javaClient = javaClient;
        _mapper = mapper;
        _logger = logger;
    }

    // ------------------------------------------------------------ lookups

    public async Task<IEnumerable<InquiryDto>> SearchRegisterableAsync(
        string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLower();

        // Only enquiries that have not already produced a student. Showing
        // the rest would be offering a click that always fails.
        var registered = await _db.Students.AsNoTracking().Select(s => s.InquiryId).ToListAsync(ct);

        var rows = await _db.Inquiries.AsNoTracking()
            .Include(i => i.Course)
            .Include(i => i.Staff)
            .Where(i => i.Status != InquiryStatus.Lost
                        && i.Status != InquiryStatus.NotInterested
                        && !registered.Contains(i.InquiryId))
            .ToListAsync(ct);

        if (q.Length > 0)
        {
            rows = rows.Where(i =>
                i.InquiryId.ToString().Contains(q)
                || (i.EnquirerName ?? string.Empty).ToLower().Contains(q)
                || (i.Email ?? string.Empty).ToLower().Contains(q)
                || (i.Phone ?? string.Empty).Contains(q)).ToList();
        }

        return _mapper.Map<IEnumerable<InquiryDto>>(rows);
    }

    public async Task<IEnumerable<BatchDto>> JoinableBatchesForCourseAsync(
        int courseId, CancellationToken ct = default)
    {
        var batches = await _db.Batches.AsNoTracking()
            .Include(b => b.Course).ThenInclude(c => c!.Category)
            .Include(b => b.Staff)
            .Where(b => b.CourseId == courseId && b.IsActive
                        && (b.Status == BatchStatus.Upcoming || b.Status == BatchStatus.Ongoing))
            .OrderBy(b => b.StartDate)
            .ToListAsync(ct);

        return await WithLiveCountsAsync(batches, ct);
    }

    public async Task<FeeBreakdownDto> FeeBreakdownForCourseAsync(
        int courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == courseId, ct)
            ?? throw new ResourceNotFoundException("Course", courseId);

        return _fees.BreakdownFor(course, DateOnly.FromDateTime(DateTime.Today));
    }

    // ------------------------------------------------------------- write

    /// <param name="bearerToken">
    /// The caller's own JWT, forwarded to the Java service when fetching the
    /// receipt PDF. Passed in rather than read from an IHttpContextAccessor
    /// so this service stays testable without a fake HTTP context — and so it
    /// is obvious at the call site that a credential is crossing a boundary.
    /// </param>
    public async Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request, string? bearerToken = null, CancellationToken ct = default)
    {
        // An explicit transaction, because five tables move together and
        // SaveChanges alone only makes each call atomic, not the sequence.
        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync(ct);

        var inquiry = await _db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == request.InquiryId, ct)
            ?? throw new ResourceNotFoundException("Enquiry", request.InquiryId);

        if (inquiry.Status is InquiryStatus.Lost or InquiryStatus.NotInterested)
        {
            throw new BusinessRuleException($"Enquiry #{inquiry.InquiryId} is closed and can't be registered.");
        }

        // BRD 4.1, the half the database cannot express: one enquiry yields
        // at most one student.
        if (await _db.Students.AnyAsync(s => s.InquiryId == inquiry.InquiryId, ct))
        {
            throw new BusinessRuleException($"Enquiry #{inquiry.InquiryId} has already been registered.");
        }

        var email = request.Student.Email.Trim();
        if (await _db.Students.AnyAsync(s => s.Email == email, ct))
        {
            throw new BusinessRuleException($"A student is already registered with the email {email}.");
        }

        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.BatchId == request.BatchId, ct)
            ?? throw new ResourceNotFoundException("Batch", request.BatchId);

        var course = await ResolveCourseAsync(request.CourseId, inquiry, ct);

        // The batch must belong to the chosen course, or the fee charged and
        // the course attended diverge.
        if (batch.CourseId != course.CourseId)
        {
            throw new BusinessRuleException(
                $"{batch.BatchName} runs {batch.Course?.Name ?? "another course"}, but you selected {course.Name}.");
        }

        if (!JoinableStatuses.Contains(batch.Status))
        {
            throw new BusinessRuleException($"{batch.BatchName} is {batch.Status} and isn't taking new students.");
        }

        // Capacity counted from live enrolments, not the cached column — a
        // counter that drifted once would let a batch overfill silently.
        var occupied = await _db.Enrollments
            .CountAsync(e => e.BatchId == batch.BatchId && e.Status != EnrollmentStatus.Dropped, ct);

        if (occupied >= batch.Capacity)
        {
            throw new BusinessRuleException($"{batch.BatchName} is full ({occupied}/{batch.Capacity}).");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var fees = _fees.BreakdownFor(course, today);

        // The enquiry is corrected to the course actually taken, so
        // course-wise reporting agrees with the enrolment figures.
        if (course.CourseId != inquiry.CourseId)
        {
            _logger.LogInformation("Enquiry #{Id} switched course at registration to {Course}",
                inquiry.InquiryId, course.Name);
            inquiry.CourseId = course.CourseId;
        }

        var student = _mapper.Map<Student>(request.Student);
        student.InquiryId = inquiry.InquiryId;
        student.RegDate = today;
        _db.Students.Add(student);
        await _db.SaveChangesAsync(ct);   // needed: the enrolment references student.StudentId

        var enrollment = new Enrollment
        {
            StudentId = student.StudentId,
            BatchId = batch.BatchId,
            InquiryId = inquiry.InquiryId,
            EnrollDate = today,
            Status = EnrollmentStatus.Active
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        var payment = BuildFirstInstallment(request, student, enrollment, fees, today);
        _db.Payments.Add(payment);

        // Recalculated, never incremented.
        batch.CurrentCount = await _db.Enrollments
            .CountAsync(e => e.BatchId == batch.BatchId && e.Status != EnrollmentStatus.Dropped, ct) + 1;

        inquiry.Status = InquiryStatus.Converted;
        inquiry.ClosureReasonId = null;

        // Any follow-up still pending is retired — the enquiry is over.
        var pending = await _db.Followups
            .Where(f => f.InquiryId == inquiry.InquiryId && f.Status == FollowupStatus.Pending)
            .ToListAsync(ct);
        foreach (var f in pending) f.Status = FollowupStatus.Done;

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Registered student #{StudentId} into '{Batch}' against enquiry #{InquiryId}",
            student.StudentId, batch.BatchName, inquiry.InquiryId);

        var studentDto = _mapper.Map<StudentDto>(student) with
        {
            EnrollmentId = enrollment.EnrollmentId,
            BatchId = batch.BatchId,
            BatchName = batch.BatchName,
            CourseName = course.Name
        };

        // Emailed AFTER the commit, deliberately.
        //
        // Two network calls happen in here — fetching the PDF from Java and
        // handing the message to an SMTP server — and neither belongs inside
        // a database transaction holding row locks on five tables. Doing this
        // before the commit would hold those locks for the duration of an
        // SMTP handshake, which is the classic way to turn a slow mail server
        // into database contention.
        var emailed = await EmailReceiptAsync(
            student, course, batch, payment, fees, bearerToken, ct);

        return new RegistrationResult(
            studentDto,
            _mapper.Map<PaymentDto>(payment),
            fees,
            $"/payments/{payment.PaymentId}/receipt",
            emailed);
    }

    /// <summary>
    /// Sends the registration confirmation, with the receipt PDF attached
    /// when the Java service can render one.
    ///
    /// Returns whether it actually went, which the confirmation screen shows.
    /// Never throws: the registration is already committed by the time this
    /// runs, so an exception here would report a failure for something that
    /// definitively succeeded.
    /// </summary>
    private async Task<bool> EmailReceiptAsync(
        Student student,
        Course course,
        Batch batch,
        Payment payment,
        FeeBreakdownDto fees,
        string? bearerToken,
        CancellationToken ct)
    {
        if (!_emailService.IsConfigured || string.IsNullOrWhiteSpace(student.Email))
        {
            return false;
        }

        var balance = fees.TotalFees - payment.Amount;

        // The balance, not the plan. A student who paid in full must not be
        // told an installment is still due — the bug we fixed on three
        // screens in the Java build started exactly here.
        var balanceLine = balance <= 0
            ? "Your fees are fully paid. Thank you."
            : $"Balance outstanding: {balance:N2}";

        var body = $"""
            Dear {student.FirstName} {student.LastName},

            Welcome to SMVITA. Your registration is confirmed.

            Course  : {course.Name}
            Batch   : {batch.BatchName}
            Receipt : {payment.ReceiptNo}
            Paid    : {payment.Amount:N2}
            {balanceLine}

            Your receipt is attached.

            Warm regards,
            Shriram Mantri Vidyanidhi Info Tech Academy
            """;

        // The PDF is rendered by the Java service — OpenPDF is a Java
        // library, and reimplementing that layout here would be duplicated
        // work with two sets of bugs.
        var pdf = await _javaClient.GetReceiptPdfAsync(payment.PaymentId, bearerToken, ct);

        if (pdf is null)
        {
            // Java unreachable is not a reason to send nothing. The student
            // still gets confirmation that they are registered, and the
            // receipt remains downloadable from the admin panel.
            _logger.LogWarning(
                "Receipt PDF unavailable for payment {PaymentId} — sending confirmation without it",
                payment.PaymentId);

            return await _emailService.SendAsync(
                student.Email,
                $"Registration confirmed — {course.Name}",
                body.Replace("Your receipt is attached.",
                             "Your receipt is available from the academy office."),
                ct);
        }

        // The receipt number contains slashes (VITA/2026-27/00012-1). Left in
        // an attachment filename, a slash is a path separator to some mail
        // clients — so it is swapped for a hyphen rather than trusted.
        var safeReceipt = string.IsNullOrWhiteSpace(payment.ReceiptNo)
            ? payment.PaymentId.ToString()
            : payment.ReceiptNo.Replace('/', '-');

        return await _emailService.SendWithAttachmentAsync(
            student.Email,
            $"Registration confirmed — {course.Name}",
            body,
            $"receipt-{safeReceipt}.pdf",
            pdf,
            "application/pdf",
            ct);
    }

    // ------------------------------------------------------------ helpers

    private async Task<Course> ResolveCourseAsync(int? requestedCourseId, Inquiry inquiry, CancellationToken ct)
    {
        if (requestedCourseId is null || requestedCourseId == inquiry.CourseId)
        {
            return await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == inquiry.CourseId, ct)
                ?? throw new ResourceNotFoundException("Course", inquiry.CourseId);
        }

        var chosen = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == requestedCourseId, ct)
            ?? throw new ResourceNotFoundException("Course", requestedCourseId.Value);

        // A course deliberately switched to must be active. The enquiry's own
        // is exempt: it was valid when the lead came in, and refusing to
        // register an existing prospect because the course was retired since
        // would strand them.
        if (!chosen.IsActive)
        {
            throw new BusinessRuleException($"{chosen.Name} is no longer offered and can't take new registrations.");
        }

        return chosen;
    }

    private Payment BuildFirstInstallment(
        RegistrationRequest request, Student student, Enrollment enrollment,
        FeeBreakdownDto fees, DateOnly today)
    {
        var expected = fees.Installment1Amount;
        var amount = request.AmountPaid ?? expected;

        // A short payment is refused rather than accepted quietly. The
        // 2-installment plan is fixed system-wide, so "they paid a bit less
        // today" has no representation in the data model — accepting it would
        // silently understate what the student owes.
        if (amount < expected)
        {
            throw new BusinessRuleException(
                $"Installment 1 is {expected}. Collecting less isn't supported — the 2-installment plan is fixed.");
        }

        if (amount > fees.TotalFees)
        {
            throw new BusinessRuleException($"That's more than the full course fee of {fees.TotalFees}.");
        }

        return new Payment
        {
            StudentId = student.StudentId,
            EnrollmentId = enrollment.EnrollmentId,
            Amount = amount,
            InstallmentNumber = 1,
            TotalInstallments = FeeCalculator.TotalInstallments,
            PaymentDate = today,
            PaymentMode = ParseMode(request.PaymentMode),
            PaymentStatus = PaymentStatus.Success,
            TransactionId = request.TransactionId,
            Remarks = request.Remarks,
            ReceiptNo = _fees.ReceiptNumberFor(student.StudentId, 1, today)
        };
    }

    internal static PaymentMode ParseMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return PaymentMode.Cash;

        // "Bank Transfer" arrives with its space; the enum member has none.
        var normalised = raw.Replace(" ", string.Empty);
        return Enum.TryParse<PaymentMode>(normalised, true, out var mode)
            ? mode
            : throw new BusinessRuleException($"Unknown payment mode: '{raw}'");
    }

    /// <summary>
    /// Replaces each batch's cached count with one derived from enrolments.
    ///
    /// Counts everything except Dropped — a student who finished the course
    /// was still in the batch, so counting only Active would report every
    /// completed batch as zero enrolled.
    /// </summary>
    private async Task<List<BatchDto>> WithLiveCountsAsync(List<Batch> batches, CancellationToken ct)
    {
        var ids = batches.Select(b => b.BatchId).ToList();

        var counts = await _db.Enrollments.AsNoTracking()
            .Where(e => ids.Contains(e.BatchId) && e.Status != EnrollmentStatus.Dropped)
            .GroupBy(e => e.BatchId)
            .Select(g => new { BatchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BatchId, x => x.Count, ct);

        // PlacedCount is left at zero here on purpose: this list is the
        // registration wizard's batch dropdown, where placement figures are
        // irrelevant and an extra query would be waste.
        return batches
            .Select(b => _mapper.Map<BatchDto>(b) with
            {
                CurrentCount = counts.TryGetValue(b.BatchId, out var n) ? n : 0
            })
            .ToList();
    }
}
