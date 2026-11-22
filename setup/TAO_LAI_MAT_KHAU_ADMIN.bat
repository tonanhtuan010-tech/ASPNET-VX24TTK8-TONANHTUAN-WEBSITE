@echo off
setlocal EnableExtensions DisableDelayedExpansion
if /I "%~1"=="--run" goto run
start "Laptop Store - Admin" "%ComSpec%" /d /k call "%~f0" --run
exit /b
:run
cd /d "%~dp0..\src\WebsiteBanLaptop"
set "ASPNETCORE_ENVIRONMENT=Development"
set "DOTNET_NOLOGO=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
dotnet build "WebsiteBanLaptop.csproj" --nologo --verbosity quiet >nul 2>&1
if errorlevel 1 (
  echo Khong the bien dich du an. Kiem tra lai bang Visual Studio.
  exit /b 1
)
dotnet "bin\Debug\net10.0\WebsiteBanLaptop.dll" --reset-admin-password
endlocal
