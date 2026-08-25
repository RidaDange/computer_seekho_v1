using System.Security.Claims;
using ComputerSeekho.Service;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Reads the signed-in staff member out of the JWT.
///
/// The identity comes from the token, never from a query parameter. That
/// distinction is the whole point: "?mine=true" says which view is wanted,
/// but *who* the caller is has to be something they cannot edit, or anyone
/// could read a colleague's pipeline by changing the URL.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>The staffId claim, or null when the caller is not staff.</summary>
    public static int? StaffId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(TokenService.StaffIdClaim);
        return int.TryParse(raw, out var id) ? id : null;
    }

    public static bool IsInAnyRole(this ClaimsPrincipal user, params string[] roles) =>
        roles.Any(user.IsInRole);

    /// <summary>
    /// Resolves "whose enquiries should this call return".
    ///
    /// Counsellors and receptionists default to their own leads; Admin and
    /// Manager default to everyone, because they oversee the pipeline
    /// rather than work it — and an Admin with no enquiries assigned would
    /// otherwise see an empty screen that looks broken.
    ///
    /// The mine flag overrides the default in either direction.
    /// </summary>
    public static int? ResolveScope(this ClaimsPrincipal user, bool? mine)
    {
        var oversees = user.IsInAnyRole("Admin", "Manager");
        var wantsOwn = mine ?? !oversees;

        return wantsOwn ? user.StaffId() : null;
    }
}
