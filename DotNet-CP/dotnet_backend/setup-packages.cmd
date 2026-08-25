@echo off
REM ---------------------------------------------------------------------------
REM One-time package setup for the .NET backend.
REM
REM Batch rather than PowerShell, because .cmd files are not subject to
REM PowerShell's execution policy — no Set-ExecutionPolicy needed.
REM
REM Run from the dotnet_backend folder:
REM     setup-packages.cmd
REM
REM Versions are deliberately not pinned. `dotnet add package` resolves the
REM newest version compatible with net10.0, which is safer than hardcoding a
REM version number that may not exist for this framework.
REM ---------------------------------------------------------------------------

setlocal
set API=backend
set TESTS=backend.Tests

echo.
echo === API packages ===
echo.

REM --- Database -------------------------------------------------------------
REM Pomelo is the MySQL provider for EF Core. Microsoft's own provider is
REM SQL Server only.
call dotnet add %API% package Pomelo.EntityFrameworkCore.MySql       || goto :failed
call dotnet add %API% package Microsoft.EntityFrameworkCore.Design   || goto :failed
call dotnet add %API% package Microsoft.EntityFrameworkCore.Tools    || goto :failed

REM --- Authentication -------------------------------------------------------
call dotnet add %API% package Microsoft.AspNetCore.Authentication.JwtBearer || goto :failed

REM The staff table already holds BCrypt hashes written by Java's
REM BCryptPasswordEncoder. BCrypt is a portable format, so this verifies the
REM existing hashes with no password reset.
call dotnet add %API% package BCrypt.Net-Next                        || goto :failed

REM --- Mapping, validation, logging ----------------------------------------
call dotnet add %API% package AutoMapper                             || goto :failed
call dotnet add %API% package FluentValidation.AspNetCore            || goto :failed
call dotnet add %API% package Serilog.AspNetCore                     || goto :failed
call dotnet add %API% package Serilog.Sinks.Console                  || goto :failed
call dotnet add %API% package Serilog.Sinks.File                     || goto :failed

echo.
echo === Test project ===
echo.

REM Created by the SDK rather than by hand, so the test csproj is correct.
if not exist "%TESTS%" (
    call dotnet new nunit -n %TESTS% -f net10.0 || goto :failed
    call dotnet add %TESTS% reference %API%     || goto :failed
)

call dotnet add %TESTS% package Moq                                  || goto :failed
REM An in-memory provider lets service tests run without a MySQL server.
call dotnet add %TESTS% package Microsoft.EntityFrameworkCore.InMemory || goto :failed

echo.
echo === Solution ===
echo.

if not exist "ComputerSeekho.slnx" (
    call dotnet new sln -n ComputerSeekho --format slnx || goto :failed
    call dotnet sln ComputerSeekho.slnx add %API%       || goto :failed
    call dotnet sln ComputerSeekho.slnx add %TESTS%     || goto :failed
)

echo.
echo === Build ===
echo.
call dotnet build

echo.
echo Done. If the build succeeded, open ComputerSeekho.slnx in Visual Studio.
echo.
goto :eof

:failed
echo.
echo SETUP FAILED at the command above. Nothing further was run.
exit /b 1
