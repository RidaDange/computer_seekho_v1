using AutoMapper;
using ComputerSeekho.DTO;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ComputerSeekho.Tests;

/// <summary>
/// Tests for the reusable CRUD of requirements 7 and 8.
///
/// Worth testing on its own rather than only through a concrete service,
/// because every master table depends on it. A bug here is a bug in nine
/// endpoints at once — which is the flip side of the reuse being worth
/// having.
///
/// Recruiter is used as the sample type. Nothing in GenericService knows
/// what a Recruiter is, so any entity would do.
/// </summary>
[TestFixture]
public class GenericServiceTests
{
    private Mock<IGenericRepository<Recruiter>> _repository = null!;
    private IMapper _mapper = null!;
    private GenericService<Recruiter, RecruiterDto> _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IGenericRepository<Recruiter>>();
        _mapper = TestMapper.Create();

        _service = new GenericService<Recruiter, RecruiterDto>(
            _repository.Object,
            _mapper,
            NullLogger<GenericService<Recruiter, RecruiterDto>>.Instance);
    }

    private static Recruiter Sample(int id = 1, string name = "Infosys") => new()
    {
        RecruiterId = id,
        CompanyName = name,
        LogoUrl = "https://example.com/logo.png",
        IsActive = true
    };

    [Test]
    public async Task GetAllAsync_MapsEveryEntityToItsDto()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([Sample(1, "Infosys"), Sample(2, "TCS")]);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].CompanyName, Is.EqualTo("Infosys"));
        Assert.That(result[1].CompanyName, Is.EqualTo("TCS"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenTheRowIsMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Recruiter?)null);

        var result = await _service.GetByIdAsync(99);

        // Null rather than an exception: "not found" is the controller's
        // decision to turn into a 404, not the service's to throw about.
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_MapsTheRequestAndSaves()
    {
        var request = new RecruiterRequest { CompanyName = "Wipro", IsActive = true };

        Recruiter? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Recruiter>(), It.IsAny<CancellationToken>()))
                   .Callback<Recruiter, CancellationToken>((e, _) => saved = e)
                   .ReturnsAsync((Recruiter e, CancellationToken _) => e);

        var result = await _service.CreateAsync(request);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.CompanyName, Is.EqualTo("Wipro"));
        Assert.That(result.CompanyName, Is.EqualTo("Wipro"));
    }

    [Test]
    public async Task UpdateAsync_MapsOntoTheExistingEntity()
    {
        // The important behaviour: mapping onto the tracked instance rather
        // than replacing it. Mapping to a fresh object would null out every
        // column the request doesn't carry, which is how a partial update
        // wipes half a row.
        var existing = Sample(1, "Old Name");
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existing);

        await _service.UpdateAsync(1, new RecruiterRequest { CompanyName = "New Name", IsActive = true });

        Assert.That(existing.CompanyName, Is.EqualTo("New Name"));
        // Same instance, not a replacement.
        Assert.That(existing.RecruiterId, Is.EqualTo(1));

        _repository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ReturnsNull_AndDoesNotSave_WhenMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Recruiter?)null);

        var result = await _service.UpdateAsync(99, new RecruiterRequest { CompanyName = "Ghost" });

        Assert.That(result, Is.Null);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Recruiter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ReportsWhetherAnythingWasRemoved()
    {
        _repository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.DeleteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.That(await _service.DeleteAsync(1), Is.True);
        Assert.That(await _service.DeleteAsync(99), Is.False);
    }

    /// <summary>
    /// RecruiterRequest has no RecruiterId, so a client cannot choose one.
    /// Checked by reflection because it is a property of the contract, not
    /// of any single call.
    /// </summary>
    [Test]
    public void RequestTypes_DoNotExposeTheIdentity()
    {
        Assert.That(typeof(RecruiterRequest).GetProperty("RecruiterId"), Is.Null);
        Assert.That(typeof(CourseCreateRequest).GetProperty("CourseId"), Is.Null);
        Assert.That(typeof(StaffCreateRequest).GetProperty("StaffId"), Is.Null);
    }
}
