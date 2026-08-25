using ComputerSeekho.DTO;

namespace ComputerSeekho.Service;

/// <summary>Stores uploaded images and returns the path they are served from.</summary>
public interface IFileStorageService
{
    Task<UploadResultDto> StoreAsync(IFormFile file, string category, CancellationToken ct = default);
}

/// <summary>
/// Writes uploads to disk and hands back the path they are served from.
///
/// Files live on the filesystem rather than in the database. Image bytes in
/// a BLOB bloat every backup of a schema that is otherwise small enough to
/// hand around, and they would stream through the application on each
/// request instead of being served straight off the static handler.
///
/// The layout matches the Java backend's, so both write to the same folder
/// and either can serve what the other stored.
/// </summary>
public class FileStorageService : IFileStorageService
{
    /// <summary>Public prefix these files are served under.</summary>
    public const string UrlPrefix = "/uploads";

    /// <summary>
    /// The extension comes from the content type, never the uploaded
    /// filename. A client-supplied name can carry a second extension
    /// ("photo.jpg.html") or path separators, and neither survives being
    /// discarded outright.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/pjpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/gif"] = "gif",
        ["image/webp"] = "webp"
    };

    /// <summary>
    /// Where an upload may land. A fixed set rather than free text: the
    /// category becomes a directory name, so accepting arbitrary input is
    /// how "../.." ends up in a path.
    /// </summary>
    private static readonly HashSet<string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        "students", "staff", "batches", "courses", "gallery",
        "banners", "testimonials", "recruiters"
    };

    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly string _root;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _root = Path.GetFullPath(configuration["Uploads:Directory"] ?? "./uploads");
        _logger = logger;

        Directory.CreateDirectory(_root);
        _logger.LogInformation("Uploads directory: {Root}", _root);
    }

    public async Task<UploadResultDto> StoreAsync(IFormFile file, string category, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessRuleException("No file was uploaded.");
        }

        if (file.Length > MaxBytes)
        {
            throw new BusinessRuleException(
                $"That file is {file.Length / 1024 / 1024.0:F1} MB — the limit is 10 MB.");
        }

        if (!Categories.Contains(category))
        {
            throw new BusinessRuleException(
                $"'{category}' isn't a valid upload category. Expected one of {string.Join(", ", Categories)}.");
        }

        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
        {
            throw new BusinessRuleException(
                "Only JPG, PNG, GIF and WebP images can be uploaded — that file is " +
                (string.IsNullOrWhiteSpace(contentType) ? "of an unknown type" : contentType) + ".");
        }

        var directory = Path.GetFullPath(Path.Combine(_root, category));

        // Belt and braces. The category whitelist already makes traversal
        // impossible; this catches the case where somebody widens that set
        // later without thinking about what a category may contain.
        if (!directory.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("Invalid upload category.");
        }

        Directory.CreateDirectory(directory);

        var filename = $"{Guid.NewGuid()}.{extension}";
        var path = Path.Combine(directory, filename);

        await using (var stream = File.Create(path))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Relative, not absolute, so the same database row works against
        // localhost and a deployed host without a rewrite.
        var url = $"{UrlPrefix}/{category}/{filename}";

        _logger.LogInformation("Stored upload {Original} ({Bytes} bytes) as {Url}",
            file.FileName, file.Length, url);

        return new UploadResultDto(url, file.FileName, file.ContentType, file.Length);
    }
}
