# ---------------------------------------------------------------- build
# The SDK image carries the compiler, NuGet and the whole toolchain; the
# runtime stage below keeps none of it. Shipping the SDK roughly triples the
# image and puts a compiler on a production host for no reason.
#
# The build context is dotnet_backend/, not dotnet_backend/backend/, because
# a solution-relative restore needs to see the project files it references.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The .csproj alone first. Docker caches each layer against the files that
# produced it, so as long as the PackageReference list hasn't changed this
# layer is reused and the slow NuGet restore is skipped — even when every
# .cs file has changed. Copying the source first would invalidate it on
# every edit, which is the single most common reason .NET image builds are
# slow.
COPY backend/backend.csproj backend/
RUN dotnet restore backend/backend.csproj

# Now the source, which changes constantly.
COPY backend/ backend/

# --no-restore because the layer above already did it; without this, restore
# runs a second time and the caching above buys nothing.
#
# The test project is deliberately not built here. A container build is the
# wrong place to discover a test failure — CI should have rejected the commit
# before an image was ever produced.
RUN dotnet publish backend/backend.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# -------------------------------------------------------------- runtime
# aspnet, not sdk: it carries the ASP.NET Core shared framework and nothing
# else. Not the -alpine variant — MailKit and the MySQL provider both pull in
# ICU and OpenSSL, and the musl workarounds cost more than the ~100 MB saved.
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# curl is not in the base image, and a HEALTHCHECK that fails because the
# command is missing looks identical to the application being down.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Created and owned before the user switch. The uploads volume mounts here
# at runtime, and Docker gives a fresh named volume the ownership of the
# directory that already exists at that path in the image. Without this the
# volume arrives owned by root and the first photo upload fails with a
# permission error that has nothing to do with the code.
#
# logs/ likewise — Serilog's file sink writes there and would otherwise
# throw on the first log line after startup.
RUN mkdir -p /app/uploads /app/logs && chown -R $APP_UID:$APP_UID /app

# $APP_UID is defined by the base image (a non-root user, uid 1654). A
# process that only reads assemblies and writes two directories has no use
# for root, and a container escape starting from uid 0 is a much shorter
# path to the host.
USER $APP_UID

# Kestrel binds to port 8080 by default in these images, but this is set
# explicitly so the value is visible here rather than inherited invisibly
# from the base image.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# /api because Program.cs calls UsePathBase("/api") — the health endpoint is
# mapped at /health and the path base puts it at /api/health. Probing
# /health alone returns 404 and the container would be reported unhealthy
# while serving perfectly.
HEALTHCHECK --interval=15s --timeout=3s --start-period=30s --retries=3 \
    CMD ["sh", "-c", "curl -fsS http://localhost:8080/api/health || exit 1"]

ENTRYPOINT ["dotnet", "backend.dll"]
