using System.ComponentModel.DataAnnotations;

namespace ComputerSeekho.DTO;

/// <summary>Credentials posted to /api/auth/login.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// What login returns.
///
/// The shape is fixed by the React client, which does
/// `dispatch(loginSuccess({ staff: data.staff, token: data.token }))`.
/// Renaming any of these three properties breaks the existing frontend, so
/// this record is effectively part of the API contract rather than an
/// internal choice.
/// </summary>
public record LoginResponse(
    string Token,
    long ExpiresInMs,
    StaffDto Staff
);
