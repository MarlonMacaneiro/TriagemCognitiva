# Requires: PowerShell 5+
Param(
    [string]$TessDataPath = (Join-Path $PSScriptRoot "..\tools\tessdata"),
    [ValidateSet('fast','best')]
    [string]$Quality = 'fast'
)

$ErrorActionPreference = 'Stop'

Write-Host "=== Tesseract setup ===" -ForegroundColor Cyan
Write-Host "TessDataPath: $TessDataPath"
Write-Host "Models quality: $Quality (eng, por)"

# Choose source repo
if ($Quality -eq 'best') {
    $BaseUrl = 'https://github.com/tesseract-ocr/tessdata_best/raw/main'
}
else {
    $BaseUrl = 'https://github.com/tesseract-ocr/tessdata_fast/raw/main'
}

$languages = @('eng','por')

# Ensure directory
New-Item -ItemType Directory -Force -Path $TessDataPath | Out-Null

function Download-IfMissing($lang) {
    $dest = Join-Path $TessDataPath "$lang.traineddata"
    if (Test-Path $dest) {
        Write-Host "Already present: $lang.traineddata" -ForegroundColor Yellow
        return
    }
    $url = "$BaseUrl/$lang.traineddata"
    Write-Host "Downloading $lang from $url" -ForegroundColor Green
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $dest
}

foreach ($l in $languages) { Download-IfMissing $l }

Write-Host ""
Write-Host "Done. Files in: $TessDataPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "How to use in code:" -ForegroundColor DarkCyan
Write-Host "  var ocr = new TesseractOcrService(@'$TessDataPath', \"por+eng\");"
Write-Host "Or set env var TESSDATA_PREFIX to '$TessDataPath' if you centralize config."

