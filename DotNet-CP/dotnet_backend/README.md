# ComputerSeekho — .NET backend

An ASP.NET Core port of the Java Spring Boot backend, reading and writing
the **same** `computerseekho` MySQL schema and serving the **same** React
frontend unchanged.

The `backend/` and `frontend/` folders at the project root are untouched.
Everything here is new.

---

## Running it

**1. Install packages** (first time only), from this folder:

```powershell
setup-packages.cmd
```

Package versions are not pinned in that script — `dotnet add package`
resolves what actually exists for `net10.0`. Two exceptions are pinned
deliberately in the csproj files; see *Version pins* below.

**2. Check the connection string** in `backend/appsettings.json`. It points
at the same database the Java backend uses.

**3. Run:**

```powershell
dotnet run --project backend
```

The API listens on **http://localhost:5192**, every route under `/api`.

**4. Point the frontend at it.** From `frontend/`, in PowerShell:

```powershell
$env:VITE_API_BASE_URL = "http://localhost:5192/api"
npm run dev
```

The variable lives only in that terminal window, so switching back to Java
is a matter of which terminal you start the frontend from. Nothing in the
React app changes.

**5. Run the tests:**

```powershell
dotnet test
```

22 tests, no database required.

### Running both backends at once

They can, and it's worth demonstrating. Java on 8080, .NET on 5192, both
against the same database. A record created through one appears immediately
in the other.

---

## How the requirements are met

| # | Requirement | Where |
|---|---|---|
| 1 | Logging | Serilog in `Program.cs`; `ILogger<T>` through the services |
| 2 | JWT | `Service/TokenService.cs`, validation in `Program.cs` |
| 3 | Microsoft.Extensions.AI | **Not implemented** — see *Outstanding* |
| 4 | Global exception middleware | `Middleware/ExceptionMiddleware.cs` |
| 5 | NUnit | `backend.Tests` — 22 tests |
| 6 | Validation both sides | Data annotations on the DTOs; React already validates |
| 7 | Generic CRUD interface | `Repository/IGenericRepository.cs`, `Service/IGenericService.cs` |
| 8 | Generic CRUD implementation | `Repository/GenericRepository.cs`, `Service/GenericService.cs`, `Controllers/GenericController.cs` |
| 9 | Employee service | `Service/EmployeeService.cs` — extends `GenericService<Staff, StaffDto>` |
| 10 | AutoMapper | `AutoMapperProfiles/MappingProfile.cs` |
| 11 | Call the Java service | `Service/JavaMicroserviceClient.cs`, `Controllers/ProxyController.cs` |

---

## What's ported

**12 entities:** Staff, CourseCategory, Course, CourseStaff, ClosureReason,
Inquiry, Recruiter, Banner, Announcement, Testimonial, NewsEvent,
GalleryImage.

**12 controllers.** Nine of them are four lines each, because
`GenericController<TEntity, TDto, TRequest>` holds the CRUD:

```csharp
[ApiController]
[Route("recruiters")]
[Authorize(Roles = "Admin,Manager")]
public class RecruiterController
    : GenericController<Recruiter, RecruiterDto, RecruiterRequest>
{
    public RecruiterController(IGenericService<Recruiter, RecruiterDto> service,
                               ILogger<RecruiterController> logger)
        : base(service, logger) { }
}
```

Three are not generic, and the reasons are worth knowing:

- **Banners and announcements** — their public read is "currently valid",
  not merely "active". Each carries start and end dates, so an expired item
  never reaches the client and no screen has to remember to hide one.
- **Courses** — `CourseDto` carries `CategoryName`, which lives on another
  table. That is the honest boundary of the generic approach: it covers
  CRUD, and stops the moment a response needs a second table.

---

## Design notes

**The frontend is the specification.** React is shared and unmodified, so
the .NET API must match the Java one exactly — same paths, same JSON
property names, same error shape. `LoginResponse` returning
`{ token, expiresInMs, staff }` is not an internal choice; the client reads
those three names directly.

**Passwords carry over.** The `staff` table holds BCrypt hashes written by
Java's `BCryptPasswordEncoder`. BCrypt embeds its salt and cost in the hash
string, so `BCrypt.Net-Next` verifies them as they are. Nobody resets a
password to switch backends.

**Enums containing illegal characters.** `In-Followup` has a hyphen and
`Not Interested` a space, so neither can be a C# identifier. `AppDbContext`
converts explicitly, mirroring Java's `InquiryStatusConverter`. Without it,
EF Core would write the integer ordinal and silently corrupt rows the Java
backend wrote.

**Database-first, never migrations.** `db/schema.sql` is the source of
truth and is shared. Nothing here creates or alters a table — the same
stance as Hibernate's `ddl-auto=validate`. The `Migrations` folder is
present for structure but is deliberately empty.

**Java is an integration, not a dependency.** The .NET API runs the whole
system on its own. `ProxyController` returns **503** rather than 500 when
Java is unreachable, because nothing here is broken — a dependency is
absent, which is a different thing for a caller to act on.

The honest reason to call Java at all is receipt rendering: OpenPDF is a
Java library, and reimplementing that layout in .NET would be duplicated
work with two sets of bugs.

---

## Version pins

Two versions are pinned against NuGet's preference, both for the same
underlying reason — a package compiled against a different major version of
a shared dependency.

**EF Core is held at 9.x** in `backend.csproj`, because
`Pomelo.EntityFrameworkCore.MySql` has no EF Core 10 release yet. Letting
`Microsoft.EntityFrameworkCore.Design` resolve to 10.x loads EF Core 10
assemblies that Pomelo was not built against, and the first database call
fails with:

```
MissingMethodException: Method not found:
'System.String ...AbstractionsStrings.ArgumentIsEmpty(System.Object)'
```

The app still targets `net10.0` and runs on the .NET 10 runtime — EF Core 9
targets `net8.0` and .NET is backward compatible.

**`Microsoft.EntityFrameworkCore.InMemory` is pinned to 9.0.0** in the test
project for the same reason. Nothing uses it yet; leaving a mismatched
version in place is a trap for whoever writes the first DbContext test.

**Known warning:** `Microsoft.OpenApi` 2.0.0 carries advisory
[GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).
It arrives transitively via `Microsoft.AspNetCore.OpenApi`. Overriding it to
3.x does **not** work — the ASP.NET Core source generator is compiled
against 2.x and fails with `CS0200: IOpenApiMediaType.Example ... is read
only`. The fix is to bump `Microsoft.AspNetCore.OpenApi` itself when a
version built against 3.x is available. In the meantime the exposure is
limited: this is a build-time document *generator*, and the vulnerability is
in document *parsing*, which this application never does.

---

## Four traps worth knowing about

All four compiled cleanly and failed only at runtime. They are recorded
because each one cost real time.

1. **EF Core version mismatch** — as above. `MissingMethodException` on a
   method you never call almost always means two packages disagree about a
   shared dependency.

2. **`[NonController]` on the generic base** — `NonControllerAttribute` is
   declared `Inherited = true`, and discovery checks it with
   `IsDefined(type)`, the overload that walks the inheritance chain. One
   attribute on the base marked all nine derived controllers as "not a
   controller", and every route silently 404'd with no error at startup. It
   was also unnecessary: discovery already skips abstract types and open
   generics. The OpenAPI document at `/api/openapi/v1.json` is what found
   it — it lists what the framework *discovered*, not what you wrote.

3. **AutoMapper and positional records** — `CourseDto` was
   `record CourseDto(int CourseId, ...)`. AutoMapper maps those through the
   constructor, and `ForMember(...).Ignore()` does not apply to constructor
   parameters. Three parameters had no source, so mapping threw. It is now a
   record with init-only properties, which makes AutoMapper use property
   mapping.

4. **AutoMapper needs `ILoggerFactory`** — `AddAutoMapper` resolves one when
   building the mapper. `WebApplication.CreateBuilder` registers logging
   automatically, so `Program.cs` never notices; a bare `ServiceCollection`
   in a test does not, and every test failed in `[SetUp]` with an error that
   had nothing to do with what it was testing. `services.AddLogging()`.

---

## Outstanding

- **Part of the domain is not ported.** Registration, payments, students,
  enrolments, batches, placements, follow-ups and uploads exist in Java
  only. The `Inquiry` entity is mapped but has no controller yet.
- **`Microsoft.Extensions.AI` is unused.** The requirement reads "for faster
  development", which may mean using AI tooling to write the code rather
  than shipping a runtime AI feature. Worth confirming before adding a
  dependency nothing calls.
- **No OAuth2 visitor sign-in.** Google login for the public enquiry form is
  Java-only so far.
- **No Docker for this backend.** The Java stack has `docker-compose.yml`;
  this one runs from the CLI.
