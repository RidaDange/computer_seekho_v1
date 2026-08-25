using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Staff records — the "Employee" endpoints of requirement 9.
///
/// Mounted at /staff so the existing React admin screens reach it
/// unchanged. Thin by design: validate, delegate, return. Every rule lives
/// in EmployeeService, and every error is turned into a response by the
/// exception middleware, which is why there is no try/catch here.
/// </summary>
[ApiController]
[Route("staff")]
[Authorize(Roles = "Admin,Manager")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StaffDto>>> GetAll(CancellationToken ct) =>
        Ok(await _employeeService.GetAllAsync(ct));

    /// <summary>
    /// Active staff of one role. Open to any signed-in user because the
    /// public Faculty page is built from it.
    /// </summary>
    [HttpGet("by-role/{role}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<StaffDto>>> GetByRole(string role, CancellationToken ct) =>
        Ok(await _employeeService.GetActiveByRoleAsync(role, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StaffDto>> GetById(int id, CancellationToken ct)
    {
        var staff = await _employeeService.GetByIdAsync(id, ct);
        return staff is null ? NotFound(new ApiError(404, $"Staff {id} was not found")) : Ok(staff);
    }

    [HttpPost]
    public async Task<ActionResult<StaffDto>> Create(StaffCreateRequest request, CancellationToken ct)
    {
        var created = await _employeeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.StaffId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StaffDto>> Update(int id, StaffCreateRequest request, CancellationToken ct)
    {
        var updated = await _employeeService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound(new ApiError(404, $"Staff {id} was not found")) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _employeeService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound(new ApiError(404, $"Staff {id} was not found"));
    }
}
