using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComputerSeekho.Controllers;

/// <summary>
/// One upload endpoint for every kind of image the admin panel handles.
///
/// Deliberately not per-entity ("POST /students/{id}/photo"): the upload has
/// to happen while the form is still being filled in, before the row exists
/// to attach it to. The client uploads first, gets a URL, and submits that
/// with the rest of the form — so no create endpoint needs a multipart
/// variant, and a validation failure elsewhere doesn't cost the photo.
/// </summary>
[ApiController]
[Route("uploads")]
[Authorize(Roles = "Admin,Manager,Counselor,Receptionist")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storage;

    public UploadController(IFileStorageService storage) => _storage = storage;

    [HttpPost]
    public async Task<ActionResult<UploadResultDto>> Upload(
        IFormFile file,
        [FromQuery] string category = "gallery",
        CancellationToken ct = default) =>
        Ok(await _storage.StoreAsync(file, category, ct));
}
