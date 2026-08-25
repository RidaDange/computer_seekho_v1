using System.Text;
using ComputerSeekho.Controllers;
using ComputerSeekho.DTO;
using ComputerSeekho.Middleware;
using ComputerSeekho.Models;
using ComputerSeekho.Repository;
using ComputerSeekho.Service;
// ClaimActionCollectionMapExtensions — which is where MapJsonKey actually
// lives. The ClaimActionCollection *type* is in
// Microsoft.AspNetCore.Authentication.OAuth.Claims, but the extension methods
// that operate on it are declared one level up, in this namespace. Importing
// the namespace the type came from is the obvious guess and does not work.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace ComputerSeekho;

/// <summary>
/// Entry point and composition root — the .NET counterpart of the Java
/// backend's Spring Boot application class plus SecurityConfig and
/// WebConfig. Everything the application is made of is registered here, and
/// the order of the pipeline below is load-bearing.
/// </summary>
public class Program
{
    private const string ReactCorsPolicy = "ReactClient";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---------------------------------------------------------- logging
        // Requirement 1. Serilog replaces the default provider rather than
        // sitting alongside it, so there is one pipeline and one format.
        // Levels come from appsettings, so they are adjustable without a
        // rebuild.
        builder.Host.UseSerilog((context, config) => config
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/computerseekho-.log", rollingInterval: RollingInterval.Day));

        // --------------------------------------------------------- database
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // ------------------------------------------------- generic plumbing
        // Requirements 7 and 8. One open-generic registration serves every
        // entity: asking for IGenericRepository<Course> yields a
        // GenericRepository<Course> with no per-entity wiring.
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));

        // Requirement 9 — the entity-specific service.
        builder.Services.AddScoped<IEmployeeService, EmployeeService>();
        builder.Services.AddScoped<ITokenService, TokenService>();

        // Services that own real business rules, as opposed to the generic
        // CRUD every master table shares.
        builder.Services.AddScoped<ICounselorAssignmentService, CounselorAssignmentService>();
        builder.Services.AddScoped<IInquiryService, InquiryService>();
        builder.Services.AddScoped<IFollowupService, FollowupService>();
        builder.Services.AddScoped<IContactMessageService, ContactMessageService>();

        // Singleton: it holds only configuration, and each send opens and
        // closes its own SmtpClient. MailKit's SmtpClient is explicitly not
        // thread-safe, so it must never be the thing that is shared.
        builder.Services.AddSingleton<IEmailService, EmailService>();

        // The transactional core. FeeCalculator is stateless, so a singleton
        // is fine and saves rebuilding it per request.
        builder.Services.AddSingleton<FeeCalculator>();
        builder.Services.AddScoped<IRegistrationService, RegistrationService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IStudentService, StudentService>();
        builder.Services.AddScoped<IBatchService, BatchService>();
        builder.Services.AddScoped<IBatchAlbumService, BatchAlbumService>();
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

        // ------------------------------------------------------- AutoMapper
        // Requirement 10. Scanning the assembly picks up every Profile, so a
        // new one is discovered by existing rather than needing registration.
        builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);

        // -------------------------------------------- Java service (req. 11)
        // AddHttpClient gives a typed client backed by IHttpClientFactory,
        // which pools and recycles handlers. A hand-constructed HttpClient
        // either exhausts sockets or goes stale on DNS changes.
        builder.Services.AddHttpClient<JavaMicroserviceClient>(client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["JavaService:BaseUrl"] ?? "http://localhost:8080");

            // Java being slow must not hold a .NET request open indefinitely.
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // -------------------------------------------------------------- JWT
        // Requirement 2.
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured");

        // JWT stays the DEFAULT scheme. Everything the React app calls carries
        // a bearer token; the cookie and Google schemes below exist only for
        // the few seconds of the sign-in handshake, and are never the default.
        var authentication = builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                    // Defaults to five minutes, meaning an expired token keeps
                    // working for five more. Zero makes expiry mean expiry.
                    ClockSkew = TimeSpan.Zero
                };
            });

        // ------------------------------------------- Google visitor sign-in
        // Gates the public enquiry form, matching the Java backend.
        //
        // Registered only when credentials are present. Calling AddGoogle with
        // an empty ClientId throws at startup, so a developer without Google
        // credentials could not run the app at all — and the rest of the API
        // has nothing to do with OAuth.
        var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId)
                            && !string.IsNullOrWhiteSpace(googleClientSecret);

        if (googleConfigured)
        {
            authentication
                .AddCookie(OAuthController.OAuthCookieScheme, options =>
                {
                    // Ten minutes is the entire budget for "click the button,
                    // pick an account, approve the consent screen". The cookie
                    // is discarded the moment the JWT is issued; this expiry
                    // only bounds an abandoned handshake.
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    options.Cookie.Name = "csk.oauth";
                    options.Cookie.HttpOnly = true;

                    // Lax, not Strict: the browser arrives here on a top-level
                    // redirect FROM google.com, and Strict would refuse to send
                    // the cookie on that cross-site navigation — which is
                    // exactly the moment it is needed.
                    options.Cookie.SameSite = SameSiteMode.Lax;
                })
                .AddGoogle(options =>
                {
                    options.ClientId = googleClientId!;
                    options.ClientSecret = googleClientSecret!;

                    // Where the handshake identity is parked between Google's
                    // redirect and our callback action.
                    options.SignInScheme = OAuthController.OAuthCookieScheme;

                    // The address that must be registered in Google Cloud
                    // Console. UsePathBase("/api") means the real URL is
                    //   http://localhost:5192/api/signin-google
                    // which is NOT the same as the Java backend's
                    //   http://localhost:8080/api/login/oauth2/code/google
                    // Both need to be listed if both backends are to work.
                    options.CallbackPath = "/signin-google";

                    // Google does not surface this one by default, and it is
                    // the claim that says whether the address was actually
                    // proved rather than merely typed.
                    options.ClaimActions.MapJsonKey("email_verified", "email_verified", "boolean");

                    // Nothing here calls a Google API on the visitor's behalf,
                    // so keeping their access token would be storing a
                    // credential with no use for it.
                    options.SaveTokens = false;
                });
        }

        builder.Services.AddAuthorization();

        // ------------------------------------------------------- MVC + CORS
        builder.Services.AddControllers();

        // Requirement 6, server side. Model binding failures are turned into
        // the same ApiError the middleware produces, so the React client's
        // single error parser handles validation failures too. Left alone,
        // ASP.NET Core returns ProblemDetails and every field error on the
        // frontend would read "Something went wrong".
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var fieldErrors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(err => $"{e.Key}: {err.ErrorMessage}"))
                    .ToList();

                return new BadRequestObjectResult(
                    new ApiError(400, "Validation failed", fieldErrors));
            };
        });

        builder.Services.AddOpenApi();

        // Matches the Java backend's /actuator/health. AddDbContextCheck
        // opens a real connection rather than only reporting that the process
        // is alive — a backend that cannot reach MySQL is not healthy, and a
        // liveness probe that says otherwise is worse than none.
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        builder.Services.AddCors(options =>
            options.AddPolicy(ReactCorsPolicy, policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        var app = builder.Build();

        // -------------------------------------------------------- pipeline
        // Order matters throughout.

        // First, so it wraps everything behind it. A handler registered
        // later cannot catch what happened before it ran.
        app.UseGlobalExceptionHandling();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // UseHttpsRedirection is deliberately absent. The React client calls
        // http://localhost:5192; redirecting to https would send the browser
        // at a self-signed dev certificate on a cross-origin hop, which
        // surfaces as an opaque CORS failure rather than anything naming the
        // real cause.

        // Every path the client knows is prefixed with /api, matching the
        // Java backend's server.servlet.context-path. Must precede routing,
        // or the prefix is still attached when the router tries to match.
        app.UsePathBase("/api");

        // Uploaded images served straight off disk.
        //
        // A separate provider rooted at the uploads folder rather than
        // wwwroot, because the files are runtime data that must survive a
        // rebuild — they are deliberately outside the published output.
        var uploadsRoot = Path.GetFullPath(
            builder.Configuration["Uploads:Directory"] ?? "./uploads");
        Directory.CreateDirectory(uploadsRoot);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
            RequestPath = "/uploads",
            // The filename is a fresh GUID on every upload, so a cached copy
            // can never be the wrong one.
            OnPrepareResponse = ctx =>
                ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
        });

        app.UseRouting();

        // Between routing and auth: earlier and the policy cannot see the
        // matched endpoint; later and a rejected request never receives its
        // CORS headers, which the browser reports as a network error rather
        // than the 401 it actually was.
        app.UseCors(ReactCorsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Anonymous, and behind the /api path base like everything else, so
        // the full URL is /api/health. A probe that needed a token would be
        // useless to Docker, which has no way to obtain one.
        app.MapHealthChecks("/health").AllowAnonymous();

        // Said once, loudly, at startup rather than discovered when a visitor
        // cannot submit the enquiry form. The endpoint requires authentication
        // either way — so without Google configured the public form returns
        // 401, and that needs to be obvious here, not mysterious there.
        if (!googleConfigured)
        {
            app.Logger.LogWarning(
                "Google visitor sign-in is NOT configured (Authentication:Google:ClientId / :ClientSecret). " +
                "The public enquiry form will reject submissions with 401 until it is.");
        }

        app.Run();
    }
}
