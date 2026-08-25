using ComputerSeekho.DTO;
using ComputerSeekho.Models;

namespace ComputerSeekho.Service;

/// <summary>
/// Requirement 9 — staff-specific business logic.
///
/// Extends the generic contract rather than replacing it: the four CRUD
/// operations come from IGenericService and only the operations that
/// genuinely need staff knowledge are declared here.
/// </summary>
public interface IEmployeeService : IGenericService<Staff, StaffDto>
{
    Task<IEnumerable<StaffDto>> GetActiveByRoleAsync(string role, CancellationToken ct = default);

    /// <summary>
    /// Verifies credentials. Returns null for any failure — unknown
    /// username, wrong password, deactivated account — so a caller cannot
    /// tell which, and neither can an attacker.
    /// </summary>
    Task<Staff?> AuthenticateAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Resolves a username to the staff record behind it — used by /auth/me
    /// to rehydrate a client that holds a token but no profile.
    /// </summary>
    Task<StaffDto?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
