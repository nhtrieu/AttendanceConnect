<#
.SYNOPSIS
    Builds AttendanceConnect in Release mode and packages it into a zip ready to copy to the target machine.
.DESCRIPTION
    Run from anywhere; resolves paths relative to this script. Produces:
    <project>\dist\AttendanceConnect-Setup.zip
#>

$ErrorActionPreference = "Stop"

$setupDir = $PSScriptRoot
$projectDir = Split-Path -Parent $setupDir
$buildOutputDir = Join-Path $projectDir "bin\Release\net48"
$distDir = Join-Path $projectDir "dist"
$packageDir = Join-Path $distDir "AttendanceConnect-Setup"

Write-Host "=== Building AttendanceConnect (Release) ===" -ForegroundColor Cyan
Push-Location $projectDir
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}
finally {
    Pop-Location
}

Write-Host "=== Packaging ===" -ForegroundColor Cyan
if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

# App files (exclude pdb to keep the package small) - AttendanceConnect.exe.config is included automatically
Get-ChildItem -Path $buildOutputDir -Exclude "*.pdb" |
    Copy-Item -Destination $packageDir -Recurse -Force

# Install scripts
Copy-Item $setupDir (Join-Path $packageDir "Setup") -Recurse -Force

$zipPath = Join-Path $distDir "AttendanceConnect-Setup.zip"
Compress-Archive -Path $packageDir -DestinationPath $zipPath -Force

Write-Host "Da tao: $zipPath" -ForegroundColor Green
Write-Host "Chuyen file zip nay sang may dich, giai nen, vao thu muc 'Setup' va chay Install.ps1 (Run as Administrator)." -ForegroundColor Green
