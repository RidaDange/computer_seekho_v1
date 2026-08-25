# ---------------------------------------------------------------------------
# One-time package setup for the .NET backend.
#
# Run from the dotnet_backend folder:
#     .\setup-packages.ps1
#
# Versions are deliberately NOT pinned. `dotnet add package` resolves the
# newest version compatible with net10.0, which is safer than hardcoding a
# version number that may not exist for this framework.
# ---------------------------------------------------------------------------

$ErrorActionPreference = "Stop"
$api = "backend"
$tests = "backend.Tests"

Write-Host "`n=== API packages ===`n" -ForegroundColor Cyan

# --- Database -------------------------------------------------------------
# Pomelo is the MySQL provider for EF Core. Microsoft's own provider is
# SQL Server only; Oracle's MySql.EntityFrameworkCore lags behind on
# EF Core releases, so Pomelo is the usual choice.
dotnet add $api package Pomelo.EntityFrameworkCore.MySql
dotnet add $api package Microsoft.EntityFrameworkCore.Design
dotnet add $api package Microsoft.EntityFrameworkCore.Tools

# --- Authentication -------------------------------------------------------
dotnet add $api package Microsoft.AspNetCore.Authentication.JwtBearer

# The staff table already holds BCrypt hashes written by Java's
# BCryptPasswordEncoder. BCrypt is a standard format, so this library
# verifies those existing hashes without any password reset.
dotnet add $api package BCrypt.Net-Next

# --- Mapping, validation, logging ----------------------------------------
dotnet add $api package AutoMapper
dotnet add $api package FluentValidation.AspNetCore
dotnet add $api package Serilog.AspNetCore
dotnet add $api package Serilog.Sinks.Console
dotnet add $api package Serilog.Sinks.File

Write-Host "`n=== Test project ===`n" -ForegroundColor Cyan

# Created here rather than by hand so the SDK writes a correct test csproj.
if (-not (Test-Path $tests)) {
    dotnet new nunit -n $tests -f net10.0
    dotnet add $tests reference $api
}

dotnet add $tests package Moq
# An in-memory provider lets the service tests run without a MySQL server.
dotnet add $tests package Microsoft.EntityFrameworkCore.InMemory

Write-Host "`n=== Solution ===`n" -ForegroundColor Cyan

if (-not (Test-Path "ComputerSeekho.slnx")) {
    dotnet new sln -n ComputerSeekho --format slnx
    dotnet sln ComputerSeekho.slnx add $api
    dotnet sln ComputerSeekho.slnx add $tests
}

Write-Host "`n=== Build ===`n" -ForegroundColor Cyan
dotnet build

Write-Host "`nDone. If the build succeeded, open ComputerSeekho.slnx in Visual Studio.`n" -ForegroundColor Green
