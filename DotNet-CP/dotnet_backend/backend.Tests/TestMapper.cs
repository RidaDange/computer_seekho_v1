using AutoMapper;
using ComputerSeekho.AutoMapperProfiles;
using Microsoft.Extensions.DependencyInjection;

namespace ComputerSeekho.Tests;

/// <summary>
/// Builds a real IMapper for the tests.
///
/// A real one rather than a Mock&lt;IMapper&gt;: mapping is behaviour worth
/// testing, not a dependency to stub out. A mocked mapper would happily
/// return whatever the test told it to and the assertions would prove
/// nothing about MappingProfile — which is exactly where the last bug was.
///
/// Constructed through the DI container rather than by calling
/// MapperConfiguration directly, because that constructor's signature has
/// changed between AutoMapper versions. Going through AddAutoMapper uses
/// the same call the application uses, so the tests cannot drift from
/// Program.cs.
/// </summary>
internal static class TestMapper
{
    private static readonly Lazy<IMapper> Instance = new(() =>
    {
        var services = new ServiceCollection();

        // AutoMapper 16 resolves an ILoggerFactory when it builds the mapper.
        // The web host registers logging as part of WebApplication.
        // CreateBuilder, so Program.cs never has to think about it — a bare
        // ServiceCollection does not, and AddAutoMapper then fails with
        // "No service for type ILoggerFactory has been registered".
        services.AddLogging();

        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    });

    public static IMapper Create() => Instance.Value;
}
