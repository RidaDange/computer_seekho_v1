using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// The five CRUD endpoints every master table needs, written once.
///
/// This is where requirements 7 and 8 finally pay off. The generic
/// repository and service removed the duplication below the controller
/// layer; without this class, nine controllers would still each repeat the
/// same GetAll / GetById / Create / Update / Delete methods with only the
/// type names changed. A derived controller now declares its route and its
/// authorisation, and nothing else.
///
/// Three type parameters, because the three shapes genuinely differ:
///   TEntity  — the table row
///   TDto     — what the API returns (no password hashes, no navigation)
///   TRequest — what the API accepts (no id, no server-owned fields)
///
/// Collapsing TDto and TRequest into one is the usual shortcut, and it is
/// how a client ends up able to set a field the server is supposed to own.
///
/// Note there is deliberately NO [NonController] attribute here.
/// NonControllerAttribute is declared with Inherited = true, and discovery
/// checks it with IsDefined(type) — the overload that walks the inheritance
/// chain. Putting it on this base therefore marks every derived controller
/// as "not a controller" too, and they all silently vanish from routing.
///
/// It is also unnecessary: ControllerFeatureProvider already skips abstract
/// types and open generics, and this class is both.
/// </summary>
public abstract class GenericController<TEntity, TDto, TRequest> : ControllerBase
    where TEntity : class
    where TDto : class
    where TRequest : class
{
    protected readonly IGenericService<TEntity, TDto> Service;
    protected readonly ILogger Logger;

    protected GenericController(IGenericService<TEntity, TDto> service, ILogger logger)
    {
        Service = service;
        Logger = logger;
    }

    /// <summary>
    /// Name of the id used in the CreatedAtAction route value. Derived
    /// controllers do not need to override it; it exists so the 201
    /// response carries a Location header that actually resolves.
    /// </summary>
    protected virtual string IdRouteName => "id";

    // Reads are anonymous, writes are not.
    //
    // This mirrors the Java SecurityConfig, where every content table's GET
    // is in PUBLIC_READ_PATHS: the website has to work for a visitor with no
    // account, and a course or a recruiter is public information. Only
    // creating, editing and deleting needs a role.
    //
    // [AllowAnonymous] on an action overrides a class-level [Authorize], so
    // a derived controller can still declare its write role at class level
    // and these two stay open.
    //
    // Note this base is only used by content and master tables. Staff
    // records are not one — EmployeeController does not derive from here,
    // precisely because a staff list carries usernames.
    [HttpGet]
    [AllowAnonymous]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetAll(CancellationToken ct) =>
        Ok(await Service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public virtual async Task<ActionResult<TDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await Service.GetByIdAsync(id, ct);
        return dto is null ? NotFoundError(id) : Ok(dto);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TDto>> Create(TRequest request, CancellationToken ct)
    {
        var created = await Service.CreateAsync(request, ct);

        // 201 with a Location header rather than a bare 200 — the client
        // gets told where the new resource lives, which is what the status
        // code is for.
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<ActionResult<TDto>> Update(int id, TRequest request, CancellationToken ct)
    {
        var updated = await Service.UpdateAsync(id, request, ct);
        return updated is null ? NotFoundError(id) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await Service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFoundError(id);
    }

    /// <summary>
    /// One place that builds the 404 body, so every controller returns the
    /// same ApiError shape the React client already parses.
    /// </summary>
    protected ActionResult NotFoundError(int id) =>
        NotFound(new ApiError(404, $"{typeof(TEntity).Name} {id} was not found"));
}
