using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>The counsellor's call list.</summary>
public interface IFollowupService
{
    /// <summary>Today and earlier — the actual call list.</summary>
    Task<IEnumerable<FollowupDto>> GetDueAsync(int? forStaffId, CancellationToken ct = default);

    /// <summary>Booked for a future date. Kept separate on purpose: a list
    /// that mixes them is no longer a call list.</summary>
    Task<IEnumerable<FollowupDto>> GetUpcomingAsync(int? forStaffId, CancellationToken ct = default);

    Task<IEnumerable<FollowupDto>> GetByInquiryAsync(int inquiryId, CancellationToken ct = default);

    Task<FollowupDto?> LogAsync(int followupId, FollowupLogRequest request, CancellationToken ct = default);

    Task<FollowupDto> ScheduleAsync(int inquiryId, int staffId, DateOnly date, CancellationToken ct = default);
}
