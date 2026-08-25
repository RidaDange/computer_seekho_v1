using System.Net;
using System.Text.Json;
using ComputerSeekho.DTO;
using ComputerSeekho.Service;

namespace ComputerSeekho.Middleware;

/// <summary>
/// Requirement 4 — global exception handling.
///
/// Sits at the very front of the pipeline so it wraps everything behind it.
/// Nothing further down needs a try/catch whose only job is turning an
/// exception into a response, which is the same reason the Java backend has
/// a single @RestControllerAdvice.
///
/// Every branch returns the same ApiError shape, because the shared React
/// client parses exactly one error format.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // camelCase, to match what the client already expects from Java.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, message) = exception switch
        {
            ResourceNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            BusinessRuleException => (HttpStatusCode.UnprocessableEntity, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Invalid username or password"),

            // Anything unmatched is a bug or an outage, not something the
            // caller did. The real message is logged; the client is told
            // nothing about internals, because an exception message can
            // carry a connection string or a SQL fragment.
            _ => (HttpStatusCode.InternalServerError,
                  _environment.IsDevelopment()
                      ? Describe(exception)
                      : "Something went wrong. Please try again.")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            // Expected refusals are not errors. Logging them at Error level
            // is how a log stops being worth reading.
            _logger.LogWarning("{Status} on {Method} {Path}: {Message}",
                (int)status, context.Request.Method, context.Request.Path, exception.Message);
        }

        // A response already begun cannot be replaced — writing a second set
        // of headers throws, masking the original exception with a confusing
        // one.
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write error body");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var body = new ApiError((int)status, message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }

    /// <summary>
    /// Flattens an exception and everything it wraps into one line.
    ///
    /// The outermost message is frequently useless on its own — AutoMapper
    /// says "Error mapping types" and puts the actual reason two levels
    /// down, and EF Core and the JSON serialiser behave the same way.
    /// Reporting only the top of the chain sends you hunting for a cause the
    /// exception was already carrying.
    ///
    /// Development only. In production this is replaced by a generic
    /// message, because an inner exception is exactly where a connection
    /// string or a SQL fragment tends to surface.
    /// </summary>
    private static string Describe(Exception exception)
    {
        var parts = new List<string>();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            parts.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join("  ->  ", parts);
    }
}

/// <summary>Registration helper, so Program.cs reads as one line.</summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionMiddleware>();
}
