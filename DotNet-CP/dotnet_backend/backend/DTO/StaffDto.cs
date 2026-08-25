using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

/// <summary>
/// A staff member as the API exposes them.
///
/// Field names and casing match the Java backend's StaffDto exactly,
/// because the React app is shared and reads these property names
/// directly. ASP.NET Core serialises to camelCase by default, which is what
/// Jackson produced on the Java side, so the wire format is identical.
///
/// PasswordHash is absent, and that absence is the point of having a DTO at
/// all — the entity carries it and it must never reach a client.
/// </summary>
public record StaffDto
{
    public int StaffId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }

    /// <summary>
    /// The role as a string, because React compares it against "Admin",
    /// "Counselor" and friends in ProtectedRoute.
    ///
    /// Init-only properties rather than a positional record so the
    /// ForMember in MappingProfile actually applies. As a positional record
    /// this happened to work — the constructor parameter resolved by name
    /// and AutoMapper converted the enum to its name — but it worked by
    /// accident, and the first member needing a real ForMember would have
    /// broken the whole mapping.
    /// </summary>
    public string? Role { get; init; }

    public string? Qualification { get; init; }
    public decimal? Experience { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Username { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Create/update payload for a staff member.
///
/// Separate from StaffDto because the two genuinely differ: this one
/// carries a plaintext password (only ever inbound) and no id. Reusing one
/// record for both directions is how a client ends up able to set fields
/// the server should own.
///
/// Requirement 6 — server-side validation. These attributes mirror the
/// rules already enforced in the React forms; the server's are the ones of
/// record, since a form field is client-side and editable.
/// </summary>
public class StaffCreateRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^$|^[6-9]\d{9}$",
        ErrorMessage = "Enter a valid 10-digit mobile number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = "Counselor";

    [MaxLength(200)]
    public string? Qualification { get; set; }

    [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years")]
    public decimal? Experience { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Optional on update — a blank value means "leave the existing hash
    /// alone" rather than "set the password to empty".
    /// </summary>
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}
