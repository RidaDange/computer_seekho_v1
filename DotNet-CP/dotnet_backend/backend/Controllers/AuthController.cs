using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Staff sign-in.
///
/// Route is "auth", not "api/auth" — Program.cs applies /api as a path base
/// for the whole application, matching the Java backend's context path.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IEmployeeService employeeService,
        ITokenService tokenService,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _employeeService = employeeService;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Verifies credentials and returns a JWT.
    ///
    /// The response shape is fixed by the React client, which reads
    /// data.token and data.staff. It is part of the API contract, not an
    /// internal detail.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var staff = await _employeeService.AuthenticateAsync(request.Username, request.Password, ct);

        if (staff is null)
        {
            // One message for every failure mode. Telling a caller which
            // half they got right is free information for someone guessing.
            return Unauthorized(new ApiError(401, "Invalid username or password"));
        }

        var (token, expiresInMs) = _tokenService.CreateStaffToken(staff);

        return Ok(new LoginResponse(token, expiresInMs, _mapper.Map<StaffDto>(staff)));
    }

    /// <summary>
    /// Rehydrates the signed-in staff member from their token, for a client
    /// that has a stored token but no profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<StaffDto>> Me(CancellationToken ct)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new ApiError(401, "No signed-in staff member on this request"));
        }

        var dto = await _employeeService.GetByUsernameAsync(username, ct);

        return dto is null
            ? Unauthorized(new ApiError(401, "Staff account no longer exists"))
            : Ok(dto);
    }
}
