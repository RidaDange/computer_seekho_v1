using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ComputerSeekho.Models;
using Microsoft.IdentityModel.Tokens;

namespace ComputerSeekho.Service;

/// <summary>
/// Requirement 2 — JWT issuing.
///
/// The claim set deliberately mirrors the Java JwtService: subject, role and
/// staff id. Carrying the role and id in the token is what lets an
/// authenticated request be authorised without a database lookup, which is
/// what makes a stateless API genuinely stateless rather than stateless in
/// name only.
/// </summary>
public class TokenService : ITokenService
{
    /// <summary>Non-standard claim, named to match the Java token.</summary>
    public const string StaffIdClaim = "staffId";

    /// <summary>
    /// The short claim name the Java backend expects. Distinct from
    /// ClaimTypes.Role, which is a schema URI only .NET understands.
    /// </summary>
    public const string RoleClaim = "role";

    /// <summary>
    /// The role carried by a Google-verified member of the public. Not a
    /// Staff.Role value, and deliberately so — a visitor has no staff record.
    /// </summary>
    public const string VisitorRole = "VISITOR";

    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly int _visitorExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured");

        // HS256 needs a 256-bit key. Checked at startup rather than on the
        // first login, so a misconfiguration is obvious immediately instead
        // of surfacing as a runtime failure the first time someone signs in.
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 characters (256 bits) for HS256");
        }

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _issuer = configuration["Jwt:Issuer"] ?? "ComputerSeekho";
        _audience = configuration["Jwt:Audience"] ?? "ComputerSeekhoClient";
        _expirationMinutes = configuration.GetValue("Jwt:ExpirationMinutes", 1440);

        // Deliberately much shorter than a staff token. A visitor token is
        // handed to an anonymous member of the public and only has to survive
        // filling in one form, so an hour is generous. A staff member is
        // working all day, so theirs lasts one.
        _visitorExpirationMinutes = configuration.GetValue("Jwt:VisitorExpirationMinutes", 60);
    }

    public (string Token, long ExpiresInMs) CreateStaffToken(Staff staff)
    {
        var expires = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, staff.Username),
            new(ClaimTypes.Name, staff.Username),
            // ClaimTypes.Role is what [Authorize(Roles = "...")] reads. Using
            // a bare "role" claim would leave every role check silently
            // failing, because ASP.NET Core would not know where to look.
            // ClaimTypes.Role is the long schema URI, and it is what
            // [Authorize(Roles = "...")] reads on this side. Without it every
            // role check here fails silently.
            new(ClaimTypes.Role, staff.Role.ToString()),

            // The same value again under the plain name "role", because the
            // Java backend's JwtAuthenticationFilter reads claims.get("role")
            // and knows nothing about Microsoft's schema URIs.
            //
            // This is what lets the receipt endpoint forward a .NET-issued
            // token to the Java service. Both claims coexist without
            // conflict: each side reads the one it understands.
            new(RoleClaim, staff.Role.ToString()),

            new(StaffIdClaim, staff.StaffId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token),
                (long)TimeSpan.FromMinutes(_expirationMinutes).TotalMilliseconds);
    }

    /// <summary>
    /// Mints a token for a member of the public who has just proved, via
    /// Google, that they own an email address.
    ///
    /// Mirrors the Java JwtService.generateVisitorToken exactly, because the
    /// same React app consumes both.
    /// </summary>
    public (string Token, long ExpiresInMs) CreateVisitorToken(string email, string? name)
    {
        var expires = DateTime.UtcNow.AddMinutes(_visitorExpirationMinutes);

        var claims = new List<Claim>
        {
            // The verified address is the identity. There is no account here
            // and no staff row — the address Google confirmed is the whole of
            // what we know about this person.
            new(JwtRegisteredClaimNames.Sub, email),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),

            // Both spellings again, for the same reason as the staff token.
            new(ClaimTypes.Role, VisitorRole),
            new(RoleClaim, VisitorRole),

            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim("name", name));
        }

        // Note what is NOT here: there is no staffId claim.
        //
        // That absence is a security property, not an oversight. Every piece
        // of code that resolves a caller to a staff record reads staffId, so
        // a visitor token can never resolve to one — even if a role check
        // were somehow bypassed, there is no identity behind it to act as.
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token),
                (long)TimeSpan.FromMinutes(_visitorExpirationMinutes).TotalMilliseconds);
    }
}
