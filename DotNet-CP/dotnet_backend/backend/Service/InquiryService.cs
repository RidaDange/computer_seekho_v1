using AutoMapper;
using ComputerSeekho.AutoMapperProfiles;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;

namespace ComputerSeekho.Service;

/// <summary>
/// Enquiry capture and lifecycle — the entry point of the whole business
/// chain, since everything downstream traces back to a row here.
/// </summary>
public class InquiryService : IInquiryService
{
    /// <summary>Days from capture to the first call. Scheduled up front so
    /// the enquiry appears on somebody's list without anyone remembering.</summary>
    private const int FirstFollowupOffsetDays = 3;

    private static readonly InquiryStatus[] ClosedStatuses =
        new[] { InquiryStatus.Lost, InquiryStatus.NotInterested };

    private static readonly System.Linq.Expressions.Expression<Func<Inquiry, object>>[] Includes =
        new System.Linq.Expressions.Expression<Func<Inquiry, object>>[]
        { i => i.Course!, i => i.Staff!, i => i.ClosureReason! };

    private readonly IGenericRepository<Inquiry> _inquiries;
    private readonly IGenericRepository<Course> _courses;
    private readonly IGenericRepository<Followup> _followups;
    private readonly IGenericRepository<ClosureReason> _closureReasons;
    private readonly ICounselorAssignmentService _assignment;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<InquiryService> _logger;

    public InquiryService(
        IGenericRepository<Inquiry> inquiries,
        IGenericRepository<Course> courses,
        IGenericRepository<Followup> followups,
        IGenericRepository<ClosureReason> closureReasons,
        ICounselorAssignmentService assignment,
        IEmailService emailService,
        IMapper mapper,
        ILogger<InquiryService> logger)
    {
        _inquiries = inquiries;
        _courses = courses;
        _followups = followups;
        _closureReasons = closureReasons;
        _assignment = assignment;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InquiryDto> CreateAsync(
        InquiryCreateRequest request, string defaultSource, CancellationToken ct = default)
    {
        var course = await _courses.GetByIdAsync(request.CourseId, ct)
            ?? throw new ResourceNotFoundException("Course", request.CourseId);

        if (!course.IsActive)
        {
            throw new BusinessRuleException($"'{course.Name}' is not currently open for enquiries.");
        }

        var counselor = await _assignment.NextCounselorAsync(ct);

        var inquiry = new Inquiry
        {
            CourseId = course.CourseId,
            StaffId = counselor?.StaffId,
            EnquirerName = request.EnquirerName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Message = request.Message,
            Source = string.IsNullOrWhiteSpace(request.Source) ? defaultSource : request.Source.Trim(),
            Status = InquiryStatus.New,
            InquiryDate = DateOnly.FromDateTime(DateTime.Today)
        };

        var saved = await _inquiries.AddAsync(inquiry, ct);

        // Only when there is somebody to own it. A follow-up row with no
        // staff violates the NOT NULL on followups.staff_id, and a call
        // nobody owns is a call nobody makes.
        if (counselor is not null)
        {
            await _followups.AddAsync(new Followup
            {
                InquiryId = saved.InquiryId,
                StaffId = counselor.StaffId,
                FollowupDate = saved.InquiryDate.AddDays(FirstFollowupOffsetDays),
                Notes = "Auto-scheduled first follow-up (enquiry date + 3 days).",
                Status = FollowupStatus.Pending
            }, ct);
        }

        _logger.LogInformation("Enquiry #{Id} created from '{Source}' for course '{Course}'",
            saved.InquiryId, saved.Source, course.Name);

        // Sent after the enquiry and its follow-up are saved, and never
        // allowed to fail either of them — SendAsync swallows and logs. The
        // enquirer's record is the deliverable; the acknowledgement is a
        // courtesy.
        await _emailService.SendAsync(
            saved.Email,
            "We've received your enquiry — SMVITA",
            $"""
             Dear {saved.EnquirerName},

             Thank you for your interest in {course.Name} at SMVITA.

             Your enquiry reference is #{saved.InquiryId}. {(counselor is null
                 ? "One of our counsellors will call you shortly."
                 : $"{counselor.Name} will be in touch with you shortly.")}

             If you'd like to reach us sooner, use the Get in Touch page on
             our website.

             Warm regards,
             Shriram Mantri Vidyanidhi Info Tech Academy
             """,
            ct);

        return await ReloadAsync(saved.InquiryId, ct)
            ?? throw new ResourceNotFoundException("Inquiry", saved.InquiryId);
    }

    public async Task<IEnumerable<InquiryDto>> GetAllAsync(int? forStaffId, CancellationToken ct = default)
    {
        var rows = await _inquiries.FindWithIncludesAsync(
            forStaffId is null ? null : i => i.StaffId == forStaffId, Includes, ct);

        return await ToDtosAsync(rows, ct);
    }

    public async Task<IEnumerable<InquiryDto>> GetActiveAsync(int? forStaffId, CancellationToken ct = default)
    {
        // Closed enquiries are excluded in the query, not in the UI. One
        // that never reaches the client cannot be shown by a screen that
        // forgot to filter.
        var rows = await _inquiries.FindWithIncludesAsync(
            i => i.Status != InquiryStatus.Lost
                 && i.Status != InquiryStatus.NotInterested
                 && (forStaffId == null || i.StaffId == forStaffId),
            Includes, ct);

        return await ToDtosAsync(rows, ct);
    }

    public async Task<InquiryDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await ReloadAsync(id, ct);

    public async Task<InquiryDto?> CloseAsync(int id, CloseInquiryRequest request, CancellationToken ct = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(id, ct);
        if (inquiry is null) return null;

        if (ClosedStatuses.Contains(inquiry.Status))
        {
            // Re-closing would overwrite the original reason, losing the only
            // record of why the enquiry ended.
            throw new BusinessRuleException($"Enquiry #{id} is already closed.");
        }

        if (inquiry.Status == InquiryStatus.Converted)
        {
            throw new BusinessRuleException($"Enquiry #{id} has been converted and can't be closed.");
        }

        if (!Enum.TryParse<InquiryStatus>(request.Status.Replace(" ", string.Empty), true, out var status)
            || !ClosedStatuses.Contains(status))
        {
            throw new BusinessRuleException($"'{request.Status}' is not a closing outcome.");
        }

        var reason = await _closureReasons.GetByIdAsync(request.ClosureReasonId, ct)
            ?? throw new ResourceNotFoundException("Closure reason", request.ClosureReasonId);

        if (!reason.IsActive)
        {
            throw new BusinessRuleException($"'{reason.ReasonText}' is no longer an available reason.");
        }

        inquiry.Status = status;
        inquiry.ClosureReasonId = reason.ReasonId;
        await _inquiries.UpdateAsync(inquiry, ct);

        // Outstanding calls are retired so nothing lingers as Pending on
        // somebody's list for an enquiry that is over.
        await RetirePendingFollowupsAsync(id, ct);

        _logger.LogInformation("Enquiry #{Id} closed as {Status}: {Reason}",
            id, MappingProfile.InquiryStatusText(status), reason.ReasonText);

        return await ReloadAsync(id, ct);
    }

    public async Task<InquiryDto?> ConvertAsync(int id, CancellationToken ct = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(id, ct);
        if (inquiry is null) return null;

        if (ClosedStatuses.Contains(inquiry.Status))
        {
            throw new BusinessRuleException($"Enquiry #{id} is closed and can't be converted.");
        }

        inquiry.Status = InquiryStatus.Converted;
        inquiry.ClosureReasonId = null;
        await _inquiries.UpdateAsync(inquiry, ct);

        await RetirePendingFollowupsAsync(id, ct);

        _logger.LogInformation("Enquiry #{Id} marked Converted", id);

        return await ReloadAsync(id, ct);
    }

    // ------------------------------------------------------------ helpers

    private async Task RetirePendingFollowupsAsync(int inquiryId, CancellationToken ct)
    {
        var pending = await _followups.FindAsync(
            f => f.InquiryId == inquiryId && f.Status == FollowupStatus.Pending, ct);

        foreach (var followup in pending)
        {
            followup.Status = FollowupStatus.Done;
            await _followups.UpdateAsync(followup, ct);
        }
    }

    private async Task<InquiryDto?> ReloadAsync(int id, CancellationToken ct)
    {
        var inquiry = await _inquiries.FirstWithIncludesAsync(i => i.InquiryId == id, Includes, ct);
        if (inquiry is null) return null;

        var dtos = await ToDtosAsync([inquiry], ct);
        return dtos.FirstOrDefault();
    }

    /// <summary>
    /// Maps and attaches each enquiry's next pending call.
    ///
    /// The follow-up dates are fetched once for the whole set rather than
    /// per row — the alternative is a query per enquiry, which is the N+1
    /// problem the Java side solves with a fetch join.
    /// </summary>
    private async Task<IEnumerable<InquiryDto>> ToDtosAsync(IEnumerable<Inquiry> rows, CancellationToken ct)
    {
        var list = rows.ToList();
        if (list.Count == 0) return [];

        var ids = list.Select(i => i.InquiryId).ToHashSet();

        var pending = (await _followups.FindAsync(
                f => f.Status == FollowupStatus.Pending && ids.Contains(f.InquiryId), ct))
            .GroupBy(f => f.InquiryId)
            .ToDictionary(g => g.Key, g => g.Min(f => f.FollowupDate));

        return list
            .OrderByDescending(i => i.InquiryDate)
            .ThenByDescending(i => i.InquiryId)
            .Select(i =>
            {
                var dto = _mapper.Map<InquiryDto>(i);
                return pending.TryGetValue(i.InquiryId, out var next)
                    ? dto with { NextFollowupDate = next }
                    : dto;
            })
            .ToList();
    }
}
