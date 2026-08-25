using ComputerSeekho.Models;

namespace ComputerSeekho.Service;

/// <summary>Issues the JWTs that authenticate every admin request.</summary>
public interface ITokenService
{
    /// <summary>Mints a token for a staff member, and reports its lifetime
    /// in milliseconds so the client can store an expiry.</summary>
    (string Token, long ExpiresInMs) CreateStaffToken(Staff staff);

    /// <summary>
    /// Mints a short-lived token for a member of the public who has signed in
    /// with Google. Carries the verified email and the VISITOR role, and
    /// deliberately no staffId.
    /// </summary>
    (string Token, long ExpiresInMs) CreateVisitorToken(string email, string? name);
}
