@echo off
setlocal enabledelayedexpansion

REM Determine repo root based on this script location
set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%" >nul

echo ===============================================
echo  Benner Cognitive Services - Dev Setup
echo ===============================================

REM Check dotnet SDK
where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] .NET SDK not found. Please install .NET 8 SDK.
  echo https://dotnet.microsoft.com/download
  goto :eof
)

set "TESSDATA=%SCRIPT_DIR%tools\tessdata"
set "SETUP_MARKER=%SCRIPT_DIR%.dev_setup_done"

REM Detect previous setup
if exist "%SETUP_MARKER%" if exist "%TESSDATA%\eng.traineddata" if exist "%TESSDATA%\por.traineddata" (
  echo.
  echo [INFO] Setup ja foi executado anteriormente. Nada a fazer.
  echo Tessdata path: "%TESSDATA%"
  echo Consulte docs\SETUP_TESSERACT.md para detalhes.
  goto :summary
)

echo.
echo [1/2] Restoring NuGet packages...
dotnet restore "%SCRIPT_DIR%Benner.CognitiveServices.sln"
if errorlevel 1 (
  echo [ERROR] Restore failed.
  goto :summary
)

echo.
echo [2/2] Setting up Tesseract tessdata (eng, por)...
if not exist "%TESSDATA%" mkdir "%TESSDATA%" >nul 2>&1

where powershell >nul 2>&1
if errorlevel 1 (
  echo [WARN] PowerShell not found in PATH. Skipping tessdata download.
  echo       You can manually run: scripts\setup-tesseract.ps1
  goto :summary
)

powershell -ExecutionPolicy Bypass -NoProfile -File "%SCRIPT_DIR%scripts\setup-tesseract.ps1" -TessDataPath "%TESSDATA%" -Quality fast
if errorlevel 1 (
  echo [WARN] Tesseract setup encountered an issue. You may rerun later.
) else (
  rem create setup marker
  type nul > "%SETUP_MARKER%" 2>nul
)

:summary
echo.
echo Done.
echo Tessdata path: "%TESSDATA%"
echo Next steps:
echo   - Build: dotnet build "%SCRIPT_DIR%Benner.CognitiveServices.sln"
echo   - Use OCR in code: new TesseractOcrService("%TESSDATA%", "por+eng")
echo   - Docs: docs\SETUP_TESSERACT.md

popd >nul
endlocal
pause
