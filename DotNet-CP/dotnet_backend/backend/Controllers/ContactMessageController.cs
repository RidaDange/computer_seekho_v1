using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>Get in Touch messages.</summary>
[ApiController]
[Route("contact-messages")]
[Authorize]
public class ContactMessageController : ControllerBase
{
    private readonly IContactMessageService _service;

    public ContactMessageController(IContactMessageService service) => _service = service;

    /// <summary>
    /// The public form. Anonymous — a visitor asking a question has no
    /// account and should not need one.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ContactMessageDto>> Submit(
        ContactMessageRequest request, CancellationToken ct)
    {
        var created = await _service.SubmitAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContactMessageDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpPut("{id:int}/mark-read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct) =>
        await _service.MarkReadAsync(id, ct)
            ? NoContent()
            : NotFound(new ApiError(404, $"Message {id} was not found"));
}
