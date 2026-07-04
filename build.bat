@echo off
echo =============================================
echo   Harmony - Build Script
echo =============================================
echo.

dotnet publish Harmony.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)

echo.
echo =============================================
echo   Build complete: publish\Harmony.exe
echo =============================================
pause
