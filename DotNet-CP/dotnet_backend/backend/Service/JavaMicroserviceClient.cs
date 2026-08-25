using System.Net.Http.Headers;

namespace ComputerSeekho.Service;

/// <summary>
/// Requirement 11 — calling the existing Java service over HTTP.
///
/// A typed client rather than a raw HttpClient: the base address, timeout
/// and headers are configured once in Program.cs, and the class receives an
/// HttpClient whose lifetime IHttpClientFactory manages. Constructing
/// HttpClient directly is the well-known way to exhaust sockets, and using
/// a single static one leaves it blind to DNS changes.
///
/// Nothing else in this application depends on this class. The .NET backend
/// runs the whole system on its own; this is an integration, not a
/// dependency — which is why every method returns a nullable result and the
/// caller decides what to do when Java is not running.
/// </summary>
public class JavaMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JavaMicroserviceClient> _logger;

    public JavaMicroserviceClient(HttpClient httpClient, ILogger<JavaMicroserviceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the active course list from the Java backend.
    ///
    /// A public read, so no token is forwarded — the Java SecurityConfig
    /// permits GET /courses/active anonymously.
    /// </summary>
    public async Task<string?> GetActiveCoursesAsync(CancellationToken ct = default) =>
        await GetStringAsync("/api/courses/active", ct);

    /// <summary>
    /// Asks the Java service to render a payment receipt as a PDF.
    ///
    /// This is the honest reason for the integration: receipt rendering
    /// lives in Java because OpenPDF is a Java library, and reimplementing
    /// that layout in .NET would be duplicated work with two sets of bugs.
    ///
    /// The caller's bearer token is forwarded, because the receipt endpoint
    /// is authenticated — a receipt carries a student's name, fees and
    /// contact details, so it is not a public document.
    /// </summary>
    public async Task<byte[]?> GetReceiptPdfAsync(int paymentId, string? bearerToken, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/payments/{paymentId}/receipt");

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(ct);
            }

            // Distinguished deliberately. "Java is down" and "Java rejected
            // the token" are different problems with different fixes, and
            // reporting both as unreachable sends you looking in the wrong
            // place — which is exactly what happened the first time this ran.
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                    or System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(
                    "Java rejected the forwarded token for receipt {PaymentId} ({Status}). " +
                    "Check that Jwt:Key here matches app.jwt.secret in the Java backend.",
                    paymentId, (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Java returned {Status} for receipt {PaymentId}",
                    (int)response.StatusCode, paymentId);
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Genuinely unreachable — connection refused, DNS, or timeout.
            _logger.LogWarning(ex, "Java service unreachable for receipt {PaymentId}", paymentId);
            return null;
        }
    }

    private async Task<string?> GetStringAsync(string path, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Java service call failed for {Path}", path);
            return null;
        }
    }
}
