@echo off
REM Build script for FM26 Ctrl+P Export Mod (Windows)

echo Building FM26 Export Mod...

cd /d "%~dp0"

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: dotnet SDK not found. Please install .NET 6.0 SDK
    exit /b 1
)

REM Restore packages
echo Restoring packages...
dotnet restore

REM Build
echo Building...
dotnet build -c Release

REM Copy output
echo Copying output...
if not exist output mkdir output
copy bin\Release\net6.0\FM26ExportMod.dll output\

echo Build complete! Output: output\FM26ExportMod.dll
pause
