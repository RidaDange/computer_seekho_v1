using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;

namespace ComputerSeekho.Service;

/// <summary>
/// Requirement 9 — the entity-specific service that calls the generic one.
///
/// Everything ordinary (list, fetch, delete) is inherited from
/// GenericService and not rewritten. What is overridden is only what staff
/// records genuinely need beyond CRUD:
///
///   - a password must be hashed with BCrypt, never stored as given
///   - username and email are unique, and a clash should be a clear refusal
///     rather than a database constraint error surfacing as a 500
///   - an update with no password must leave the existing hash alone
///
/// That is the shape the requirement is describing: specific logic on top
/// of shared machinery, rather than a parallel implementation of it.
/// </summary>
public class EmployeeService : GenericService<Staff, StaffDto>, IEmployeeService
{
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IGenericRepository<Staff> repository,
        IMapper mapper,
        ILogger<GenericService<Staff, StaffDto>> baseLogger,
        ILogger<EmployeeService> logger)
        : base(repository, mapper, baseLogger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<StaffDto>> GetActiveByRoleAsync(string role, CancellationToken ct = default)
    {
        if (!Enum.TryParse<StaffRole>(role, ignoreCase: true, out var parsed))
        {
            throw new BusinessRuleException($"'{role}' is not a valid role");
        }

        var staff = await Repository.FindAsync(s => s.Role == parsed && s.IsActive, ct);
        return Mapper.Map<IEnumerable<StaffDto>>(staff);
    }

    public async Task<Staff?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var staff = await Repository.FirstOrDefaultAsync(s => s.Username == username, ct);

        if (staff is null)
        {
            // Logged, but the caller is told nothing that distinguishes this
            // from a wrong password — otherwise the endpoint becomes a way
            // to discover which usernames exist.
            _logger.LogWarning("Failed login: no account for username {Username}", username);
            return null;
        }

        // Verifies the hash Java's BCryptPasswordEncoder wrote. BCrypt
        // embeds its salt and cost in the hash string, so no extra state is
        // needed and the two backends interoperate on the same rows.
        if (!BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
        {
            _logger.LogWarning("Failed login: bad password for {Username}", username);
            return null;
        }

        if (!staff.IsActive)
        {
            _logger.LogWarning("Failed login: {Username} is deactivated", username);
            return null;
        }

        _logger.LogInformation("Staff {Username} ({Role}) logged in", staff.Username, staff.Role);
        return staff;
    }

    public async Task<StaffDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var staff = await Repository.FirstOrDefaultAsync(s => s.Username == username, ct);
        return staff is null ? null : Mapper.Map<StaffDto>(staff);
    }

    public override async Task<StaffDto> CreateAsync<TRequest>(TRequest request, CancellationToken ct = default)
    {
        if (request is not StaffCreateRequest staffRequest)
        {
            throw new ArgumentException("Expected a StaffCreateRequest");
        }

        await EnsureUniqueAsync(staffRequest, existingId: null, ct);

        if (string.IsNullOrWhiteSpace(staffRequest.Password))
        {
            throw new BusinessRuleException("A password is required when creating a staff account");
        }

        var entity = Mapper.Map<Staff>(staffRequest);
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(staffRequest.Password);

        var saved = await Repository.AddAsync(entity, ct);
        _logger.LogInformation("Created staff {Username} with role {Role}", saved.Username, saved.Role);

        return Mapper.Map<StaffDto>(saved);
    }

    public override async Task<StaffDto?> UpdateAsync<TRequest>(object id, TRequest request, CancellationToken ct = default)
    {
        if (request is not StaffCreateRequest staffRequest)
        {
            throw new ArgumentException("Expected a StaffCreateRequest");
        }

        var existing = await Repository.GetByIdAsync(id, ct);
        if (existing is null) return null;

        await EnsureUniqueAsync(staffRequest, existingId: existing.StaffId, ct);

        // Hash captured before mapping: AutoMapper ignores PasswordHash, but
        // relying on that from here would make this method silently wrong
        // the day someone "tidies up" the profile.
        var currentHash = existing.PasswordHash;
        Mapper.Map(staffRequest, existing);

        // A blank password on an update means "leave it alone", not "set the
        // password to empty". Getting this backwards locks the account out.
        existing.PasswordHash = string.IsNullOrWhiteSpace(staffRequest.Password)
            ? currentHash
            : BCrypt.Net.BCrypt.HashPassword(staffRequest.Password);

        await Repository.UpdateAsync(existing, ct);
        _logger.LogInformation("Updated staff {StaffId}", existing.StaffId);

        return Mapper.Map<StaffDto>(existing);
    }

    /// <summary>
    /// Checked in the service rather than left to the unique indexes.
    /// The database would refuse it either way, but as a constraint
    /// violation that reaches the client as an opaque 500 instead of a
    /// sentence naming the field.
    /// </summary>
    private async Task EnsureUniqueAsync(StaffCreateRequest request, int? existingId, CancellationToken ct)
    {
        var usernameTaken = await Repository.ExistsAsync(
            s => s.Username == request.Username && (existingId == null || s.StaffId != existingId), ct);

        if (usernameTaken)
        {
            throw new BusinessRuleException($"The username '{request.Username}' is already in use");
        }

        var emailTaken = await Repository.ExistsAsync(
            s => s.Email == request.Email && (existingId == null || s.StaffId != existingId), ct);

        if (emailTaken)
        {
            throw new BusinessRuleException($"The email '{request.Email}' is already in use");
        }
    }
}
