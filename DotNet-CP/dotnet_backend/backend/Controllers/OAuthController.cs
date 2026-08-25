using System.Security.Claims;
using ComputerSeekho.DTO;
using ComputerSeekho.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ComputerSeekho.Controllers;

/// <summary>
/// Google sign-in for visitors on the public website.
///
/// The React app is shared with the Java backend and unmodified, so the URLs
/// here are not a design choice — they are a contract. visitorSession.js
/// sends the browser to:
///
///     {API_BASE}/oauth2/authorization/google
///
/// which is Spring Security's convention, not ASP.NET Core's. Matching it
/// exactly is what lets the same frontend talk to either backend by changing
/// one environment variable.
///
/// The flow, end to end:
///
///   1. Browser hits /oauth2/authorization/google
///   2. We Challenge the Google handler, which redirects to Google
///   3. Google returns to /api/signin-google, handled by the middleware
///   4. Middleware signs the user into a short-lived cookie, then redirects
///      to /oauth2/callback/google below
///   5. We read the verified email, mint a visitor JWT, drop the cookie and
///      redirect to the React callback page with the token on the query string
///
/// Step 5 is where this stops being a cookie-based login and becomes the same
/// stateless JWT the rest of the API uses.
/// </summary>
[ApiController]
[AllowAnonymous]
public class OAuthController : ControllerBase
{
    /// <summary>
    /// The cookie scheme used only for the Google handshake. OAuth needs
    /// somewhere to keep the state and nonce values between the redirect out
    /// and the redirect back, and a cookie is the only place available —
    /// there is no request to attach a bearer token to when Google calls us.
    /// </summary>
    public const string OAuthCookieScheme = "OAuthHandshake";

    private readonly ITokenService _tokenService;
    private readonly ILogger<OAuthController> _logger;
    private readonly string _frontendCallback;
    private readonly bool _googleConfigured;

    public OAuthController(
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<OAuthController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;

        _frontendCallback = configuration["OAuth2:RedirectUri"]
            ?? "http://localhost:5173/oauth/callback";

        _googleConfigured = !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
                         && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);
    }

    /// <summary>
    /// Where the "Sign in with Google" button sends the browser.
    ///
    /// This is a full-page navigation, not an XHR — which is why it is a GET
    /// that returns a redirect rather than JSON. An OAuth handshake cannot
    /// happen inside fetch: the user has to actually visit Google.
    /// </summary>
    [HttpGet("oauth2/authorization/google")]
    public IActionResult StartGoogleSignIn()
    {
        if (!_googleConfigured)
        {
            // 503 rather than 500. Nothing is broken — a credential was never
            // supplied. The distinction matters to whoever reads the log.
            _logger.LogWarning(
                "Google sign-in was requested but Authentication:Google is not configured");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiError(503,
                "Visitor sign-in isn't configured on this server."));
        }

        // RedirectUri here is where Google's handler sends the browser once it
        // has finished, NOT the address registered with Google. That one is
        // CallbackPath in Program.cs.
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Reached after Google has authenticated the visitor and the middleware
    /// has written the handshake cookie.
    /// </summary>
    [HttpGet("oauth2/callback/google")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(OAuthCookieScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            _logger.LogWarning("Google sign-in did not complete: {Failure}",
                result.Failure?.Message ?? "no principal");
            return RedirectToFrontend(error: "signin_failed");
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);

        // The handshake cookie has done its one job. Dropping it here keeps
        // this genuinely stateless — from this point the visitor is carrying
        // a JWT like everyone else, and a stale sign-in cookie left behind
        // would be a second, longer-lived way in that nothing checks.
        await HttpContext.SignOutAsync(OAuthCookieScheme);

        if (string.IsNullOrWhiteSpace(email))
        {
            // Every Google account has an address, but the scope can be
            // withheld at the consent screen. Without it there is nothing to
            // file an enquiry against, so this is a failure rather than a
            // partial success.
            _logger.LogWarning("Google sign-in returned no email claim; rejecting");
            return RedirectToFrontend(error: "no_email");
        }

        // Google marks an address unverified when it has not confirmed
        // ownership. Accepting one would defeat the entire point of requiring
        // the sign-in — anyone could enquire as anyone.
        var verified = result.Principal.FindFirstValue("email_verified");
        if (string.Equals(verified, "false", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Google sign-in for {Email} has an unverified address; rejecting", email);
            return RedirectToFrontend(error: "email_unverified");
        }

        var (token, expiresInMs) = _tokenService.CreateVisitorToken(email, name);
        _logger.LogInformation("Visitor '{Email}' signed in via Google", email);

        return RedirectToFrontend(token: token, email: email, name: name, expiresInMs: expiresInMs);
    }

    // ------------------------------------------------------------- helpers

    /// <summary>
    /// Builds the redirect back into the React app.
    ///
    /// The query-string names — token, email, name, expiresInMs, error — are
    /// read verbatim by OAuthCallback.jsx, and the error values map to the
    /// messages it already renders. Renaming any of them silently breaks the
    /// page with no server-side error at all.
    ///
    /// QueryHelpers does the encoding, so an address with a '+' in it or a
    /// name with a space survives the trip.
    /// </summary>
    private IActionResult RedirectToFrontend(
        string? token = null,
        string? email = null,
        string? name = null,
        long expiresInMs = 0,
        string? error = null)
    {
        var query = new Dictionary<string, string?>();

        if (error is not null)
        {
            query["error"] = error;
        }
        else
        {
            query["token"] = token;
            query["email"] = email;
            query["name"] = name ?? string.Empty;
            query["expiresInMs"] = expiresInMs.ToString();
        }

        return Redirect(QueryHelpers.AddQueryString(_frontendCallback, query));
    }
}
