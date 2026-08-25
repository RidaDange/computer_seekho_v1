using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;

namespace ComputerSeekho.Service;

/// <summary>
/// Scheduling and logging calls.
/// </summary>
public class FollowupService : IFollowupService
{
    /// <summary>
    /// Used for in-memory checks only. Deliberately NOT used inside an EF
    /// predicate: InquiryStatus has a ValueConverter, and Contains() over a
    /// converted enum is not reliably translatable — the query predicates
    /// below spell the comparisons out instead.
    /// </summary>
    private static readonly InquiryStatus[] ClosedOrConverted =
        new[] { InquiryStatus.Lost, InquiryStatus.NotInterested, InquiryStatus.Converted };

    private static readonly System.Linq.Expressions.Expression<Func<Followup, object>>[] Includes =
        new System.Linq.Expressions.Expression<Func<Followup, object>>[]
        { f => f.Inquiry!, f => f.Inquiry!.Course!, f => f.Staff! };

    private readonly IGenericRepository<Followup> _followups;
    private readonly IGenericRepository<Inquiry> _inquiries;
    private readonly IMapper _mapper;
    private readonly ILogger<FollowupService> _logger;

    public FollowupService(
        IGenericRepository<Followup> followups,
        IGenericRepository<Inquiry> inquiries,
        IMapper mapper,
        ILogger<FollowupService> logger)
    {
        _followups = followups;
        _inquiries = inquiries;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<FollowupDto>> GetDueAsync(int? forStaffId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // The closed-enquiry filter is in the query. A closed enquiry never
        // reaches the client, so no screen has to remember to hide it.
        var rows = await _followups.FindWithIncludesAsync(
            f => f.Status == FollowupStatus.Pending
                 && f.FollowupDate <= today
                 && f.Inquiry != null
                 && f.Inquiry.Status != InquiryStatus.Lost
                 && f.Inquiry.Status != InquiryStatus.NotInterested
                 && f.Inquiry.Status != InquiryStatus.Converted
                 && (forStaffId == null || f.StaffId == forStaffId),
            Includes, ct);

        return ToDtos(rows);
    }

    public async Task<IEnumerable<FollowupDto>> GetUpcomingAsync(int? forStaffId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = await _followups.FindWithIncludesAsync(
            f => f.Status == FollowupStatus.Pending
                 && f.FollowupDate > today
                 && f.Inquiry != null
                 && f.Inquiry.Status != InquiryStatus.Lost
                 && f.Inquiry.Status != InquiryStatus.NotInterested
                 && f.Inquiry.Status != InquiryStatus.Converted
                 && (forStaffId == null || f.StaffId == forStaffId),
            Includes, ct);

        return ToDtos(rows);
    }

    public async Task<IEnumerable<FollowupDto>> GetByInquiryAsync(int inquiryId, CancellationToken ct = default)
    {
        var rows = await _followups.FindWithIncludesAsync(f => f.InquiryId == inquiryId, Includes, ct);
        return ToDtos(rows);
    }

    public async Task<FollowupDto?> LogAsync(int followupId, FollowupLogRequest request, CancellationToken ct = default)
    {
        var followup = await _followups.GetByIdAsync(followupId, ct);
        if (followup is null) return null;

        if (followup.Status != FollowupStatus.Pending)
        {
            throw new BusinessRuleException("That follow-up has already been logged.");
        }

        // "No Response" arrives with its space; the C# enum member is
        // NoResponse. Stripping spaces is the same accommodation the value
        // converter makes in the other direction.
        var outcome = request.Status.Replace(" ", string.Empty);
        if (!Enum.TryParse<FollowupStatus>(outcome, true, out var status) || status == FollowupStatus.Pending)
        {
            throw new BusinessRuleException($"'{request.Status}' is not a valid outcome.");
        }

        followup.Status = status;
        followup.Notes = request.Notes;
        followup.NextFollowup = request.NextFollowup;
        await _followups.UpdateAsync(followup, ct);

        var inquiry = await _inquiries.GetByIdAsync(followup.InquiryId, ct);

        // New becomes In-Followup on the *first* logged attempt only. An
        // enquiry already in follow-up must not be reset, and a converted or
        // closed one must not be dragged back into the pipeline.
        if (inquiry is not null && inquiry.Status == InquiryStatus.New)
        {
            inquiry.Status = InquiryStatus.InFollowup;
            await _inquiries.UpdateAsync(inquiry, ct);
            _logger.LogInformation("Enquiry #{Id} moved New -> In-Followup", inquiry.InquiryId);
        }

        if (request.NextFollowup is { } next)
        {
            await _followups.AddAsync(new Followup
            {
                InquiryId = followup.InquiryId,
                StaffId = followup.StaffId,
                FollowupDate = next,
                Status = FollowupStatus.Pending
            }, ct);
        }

        _logger.LogInformation("Follow-up #{Id} logged as {Status}", followupId, status);

        var reloaded = await _followups.FirstWithIncludesAsync(f => f.FollowupId == followupId, Includes, ct);
        return reloaded is null ? null : ToDtos([reloaded]).First();
    }

    public async Task<FollowupDto> ScheduleAsync(
        int inquiryId, int staffId, DateOnly date, CancellationToken ct = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(inquiryId, ct)
            ?? throw new ResourceNotFoundException("Inquiry", inquiryId);

        if (ClosedOrConverted.Contains(inquiry.Status))
        {
            throw new BusinessRuleException($"Enquiry #{inquiryId} is closed — there is nothing to follow up.");
        }

        var created = await _followups.AddAsync(new Followup
        {
            InquiryId = inquiryId,
            StaffId = staffId,
            FollowupDate = date,
            Status = FollowupStatus.Pending
        }, ct);

        // Scheduling also claims the enquiry when nobody owned it — which is
        // the situation this exists to recover from.
        if (inquiry.StaffId is null)
        {
            inquiry.StaffId = staffId;
            await _inquiries.UpdateAsync(inquiry, ct);
        }

        var reloaded = await _followups.FirstWithIncludesAsync(f => f.FollowupId == created.FollowupId, Includes, ct);
        return ToDtos([reloaded!]).First();
    }

    /// <summary>
    /// Maps and computes how overdue each call is.
    ///
    /// DaysOverdue is calculated here rather than in the mapping profile
    /// because it depends on today's date, which a mapping has no business
    /// knowing about.
    /// </summary>
    private List<FollowupDto> ToDtos(IEnumerable<Followup> rows)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return rows
            .OrderBy(f => f.FollowupDate)
            .ThenBy(f => f.FollowupId)
            .Select(f => _mapper.Map<FollowupDto>(f) with
            {
                DaysOverdue = today.DayNumber - f.FollowupDate.DayNumber
            })
            .ToList();
    }
}
