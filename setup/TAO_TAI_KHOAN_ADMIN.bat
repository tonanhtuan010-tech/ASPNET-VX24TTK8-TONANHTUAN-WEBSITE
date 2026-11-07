@echo off
setlocal
cd /d "%~dp0.."
set "DOTNET_NOLOGO=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "ASPNETCORE_ENVIRONMENT=Development"
dotnet run --no-launch-profile --verbosity quiet --project "src\WebsiteBanLaptop\WebsiteBanLaptop.csproj" -- --reset-admin-password
if errorlevel 1 (
    echo Khong the tao lai mat khau Admin.
)
pause >nul
endlocal
