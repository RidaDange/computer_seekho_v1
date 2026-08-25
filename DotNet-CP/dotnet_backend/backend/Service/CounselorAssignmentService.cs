using ComputerSeekho.Models;
using ComputerSeekho.Repository;

namespace ComputerSeekho.Service;

/// <summary>
/// Least-loaded round robin.
///
/// Load counts only *open* enquiries — New and In-Followup. Counting
/// everything would mean a counsellor who has closed a hundred leads looks
/// permanently busiest and stops receiving work, which is the opposite of
/// what load balancing is for.
///
/// Ties are broken by whoever was assigned least recently, so an idle team
/// rotates instead of every enquiry landing on the lowest staff id.
/// </summary>
public class CounselorAssignmentService : ICounselorAssignmentService
{
    /// <summary>An enquiry still being worked. Closed ones carry no load.</summary>
    private static readonly InquiryStatus[] OpenStatuses =
        new[] { InquiryStatus.New, InquiryStatus.InFollowup };

    private readonly IGenericRepository<Staff> _staff;
    private readonly IGenericRepository<Inquiry> _inquiries;
    private readonly ILogger<CounselorAssignmentService> _logger;

    public CounselorAssignmentService(
        IGenericRepository<Staff> staff,
        IGenericRepository<Inquiry> inquiries,
        ILogger<CounselorAssignmentService> logger)
    {
        _staff = staff;
        _inquiries = inquiries;
        _logger = logger;
    }

    public async Task<Staff?> NextCounselorAsync(CancellationToken ct = default)
    {
        var counselors = (await _staff.FindAsync(
            s => s.Role == StaffRole.Counselor && s.IsActive, ct)).ToList();

        if (counselors.Count == 0)
        {
            // Not an error. The enquiry is still captured; the enquiry list's
            // Schedule link is how it gets picked up later.
            _logger.LogWarning("No active counsellor to assign — enquiry will be unassigned");
            return null;
        }

        // One read rather than a count query per counsellor. The open set is
        // small by definition, and N queries for N counsellors is a poor
        // trade for a list that fits in memory.
        var openInquiries = (await _inquiries.FindAsync(
            i => (i.Status == InquiryStatus.New || i.Status == InquiryStatus.InFollowup)
                 && i.StaffId != null, ct)).ToList();

        var chosen = counselors
            .OrderBy(c => openInquiries.Count(i => i.StaffId == c.StaffId))
            // Least recently assigned wins a tie. Using the highest inquiry
            // id they hold as a proxy for recency: a counsellor with none at
            // all sorts first, which is what should happen.
            .ThenBy(c => openInquiries
                .Where(i => i.StaffId == c.StaffId)
                .Select(i => (int?)i.InquiryId)
                .Max() ?? 0)
            .First();

        _logger.LogInformation("Assigning enquiry to {Counselor} (staff {StaffId})",
            chosen.Name, chosen.StaffId);

        return chosen;
    }
}
