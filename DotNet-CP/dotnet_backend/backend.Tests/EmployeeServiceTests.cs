using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ComputerSeekho.Tests;

/// <summary>
/// Tests for the entity-specific business logic of requirement 9.
///
/// The repository is mocked, so none of this touches MySQL. That is the
/// payoff of putting IGenericRepository between the service and EF Core:
/// the rules can be tested in isolation, and a failure here means the rule
/// is wrong rather than the database being unavailable.
/// </summary>
[TestFixture]
public class EmployeeServiceTests
{
    private Mock<IGenericRepository<Staff>> _repository = null!;
    private IMapper _mapper = null!;
    private EmployeeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IGenericRepository<Staff>>();
        _mapper = TestMapper.Create();

        _service = new EmployeeService(
            _repository.Object,
            _mapper,
            NullLogger<GenericService<Staff, StaffDto>>.Instance,
            NullLogger<EmployeeService>.Instance);
    }

    // ------------------------------------------------------------- helpers

    private static StaffCreateRequest ValidRequest(string? password = "Secret123") => new()
    {
        Name = "Priya Sharma",
        Email = "priya.sharma@vita.com",
        Phone = "9820011111",
        Role = "Counselor",
        Username = "priya",
        Password = password,
        IsActive = true
    };

    private static Staff ExistingStaff(string password = "Secret123") => new()
    {
        StaffId = 1,
        Name = "Priya Sharma",
        Email = "priya.sharma@vita.com",
        Username = "priya",
        Role = StaffRole.Counselor,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        IsActive = true
    };

    /// <summary>Neither the username nor the email is taken.</summary>
    private void SetUpNoClashes() =>
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                      It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    // -------------------------------------------------------------- create

    [Test]
    public async Task CreateAsync_HashesThePassword()
    {
        SetUpNoClashes();

        Staff? saved = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()))
            .Callback<Staff, CancellationToken>((s, _) => saved = s)
            .ReturnsAsync((Staff s, CancellationToken _) => s);

        await _service.CreateAsync(ValidRequest());

        Assert.That(saved, Is.Not.Null);
        // The plaintext must never reach the column...
        Assert.That(saved!.PasswordHash, Is.Not.EqualTo("Secret123"));
        // ...but the hash must still verify against it.
        Assert.That(BCrypt.Net.BCrypt.Verify("Secret123", saved.PasswordHash), Is.True);
    }

    [Test]
    public void CreateAsync_RefusesADuplicateUsername()
    {
        // First ExistsAsync call is the username check.
        _repository
            .SetupSequence(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(ValidRequest()));

        // The message must name the field. A bare "duplicate" tells the
        // person at the counter nothing about what to change.
        Assert.That(ex!.Message, Does.Contain("priya"));
    }

    [Test]
    public void CreateAsync_RefusesADuplicateEmail()
    {
        _repository
            .SetupSequence(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)   // username is free
            .ReturnsAsync(true);   // email is taken

        var ex = Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(ValidRequest()));

        Assert.That(ex!.Message, Does.Contain("priya.sharma@vita.com"));
    }

    [Test]
    public void CreateAsync_RequiresAPassword()
    {
        SetUpNoClashes();

        Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(ValidRequest(password: null)));
    }

    // -------------------------------------------------------------- update

    [Test]
    public async Task UpdateAsync_WithBlankPassword_KeepsTheExistingHash()
    {
        // The rule that matters most here. Getting it backwards means
        // editing someone's phone number silently locks them out.
        var existing = ExistingStaff("OriginalPass");
        var originalHash = existing.PasswordHash;

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existing);
        SetUpNoClashes();

        await _service.UpdateAsync(1, ValidRequest(password: null));

        Assert.That(existing.PasswordHash, Is.EqualTo(originalHash));
        Assert.That(BCrypt.Net.BCrypt.Verify("OriginalPass", existing.PasswordHash), Is.True);
    }

    [Test]
    public async Task UpdateAsync_WithNewPassword_ReplacesTheHash()
    {
        var existing = ExistingStaff("OriginalPass");
        var originalHash = existing.PasswordHash;

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existing);
        SetUpNoClashes();

        await _service.UpdateAsync(1, ValidRequest(password: "BrandNewPass"));

        Assert.That(existing.PasswordHash, Is.Not.EqualTo(originalHash));
        Assert.That(BCrypt.Net.BCrypt.Verify("BrandNewPass", existing.PasswordHash), Is.True);
    }

    [Test]
    public async Task UpdateAsync_ReturnsNull_WhenTheIdIsUnknown()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Staff?)null);

        var result = await _service.UpdateAsync(99, ValidRequest());

        Assert.That(result, Is.Null);
    }

    // ------------------------------------------------------ authentication

    [Test]
    public async Task AuthenticateAsync_ReturnsTheStaff_OnCorrectCredentials()
    {
        var staff = ExistingStaff("Priya@123");
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var result = await _service.AuthenticateAsync("priya", "Priya@123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo("priya"));
    }

    [Test]
    public async Task AuthenticateAsync_ReturnsNull_ForAnUnknownUsername()
    {
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        var result = await _service.AuthenticateAsync("nobody", "whatever");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_ReturnsNull_OnAWrongPassword()
    {
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingStaff("Priya@123"));

        var result = await _service.AuthenticateAsync("priya", "WrongPassword");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_ReturnsNull_ForADeactivatedAccount()
    {
        // Deliberately indistinguishable from a wrong password to the
        // caller, so the endpoint cannot be used to discover which accounts
        // exist.
        var staff = ExistingStaff("Priya@123");
        staff.IsActive = false;

        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var result = await _service.AuthenticateAsync("priya", "Priya@123");

        Assert.That(result, Is.Null);
    }

    // -------------------------------------------------------------- by role

    [Test]
    public void GetActiveByRoleAsync_RejectsAnUnknownRole()
    {
        Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.GetActiveByRoleAsync("Janitor"));
    }

    [Test]
    public async Task GetActiveByRoleAsync_AcceptsAnyCasing()
    {
        _repository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                    It.IsAny<CancellationToken>()))
            .ReturnsAsync([ExistingStaff()]);

        var result = await _service.GetActiveByRoleAsync("cOuNsElOr");

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    // ---------------------------------------------------------- DTO safety

    [Test]
    public async Task StaffDto_NeverCarriesThePasswordHash()
    {
        // The whole reason DTOs exist. If AutoMapper is ever reconfigured
        // to map every member, this is what catches it.
        var staff = ExistingStaff("Priya@123");
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var dto = await _service.GetByUsernameAsync("priya");

        Assert.That(dto, Is.Not.Null);
        Assert.That(typeof(StaffDto).GetProperty("PasswordHash"), Is.Null,
            "StaffDto must not expose a password hash");
    }

    /// <summary>Role has to reach the client as the string React compares
    /// against in ProtectedRoute, not as an enum ordinal.</summary>
    [Test]
    public async Task StaffDto_CarriesTheRoleAsAString()
    {
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Staff, bool>>>(),
                                              It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingStaff());

        var dto = await _service.GetByUsernameAsync("priya");

        Assert.That(dto!.Role, Is.EqualTo("Counselor"));
    }
}
