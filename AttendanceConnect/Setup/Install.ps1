#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs AttendanceConnect on this machine.
.DESCRIPTION
    Run this script from the folder that contains AttendanceConnect.exe and its dependencies
    (i.e. the build/publish output, with this Setup folder copied alongside it).
#>
param(
    [string]$InstallDir = "$env:ProgramFiles\AttendanceConnect"
)

$ErrorActionPreference = "Stop"
$sourceDir = Split-Path -Parent $PSScriptRoot

Write-Host "=== AttendanceConnect Setup ===" -ForegroundColor Cyan

# 1. Check .NET Framework 4.8 (release >= 528040 corresponds to 4.8)
$releaseKey = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release -ErrorAction SilentlyContinue
if (-not $releaseKey -or $releaseKey.Release -lt 528040) {
    Write-Warning ".NET Framework 4.8 chua duoc cai tren may nay."
    Write-Warning "Tai va cai tai: https://dotnet.microsoft.com/download/dotnet-framework/net48"
    $continue = Read-Host "Tiep tuc cai dat AttendanceConnect? (y/n)"
    if ($continue -ne "y") { exit 1 }
}

# 2. Check zkemkeeper.dll (ZKTeco SDK) registration
if (-not (Test-Path "HKLM:\SOFTWARE\Classes\zkemkeeper.ZKEM.1")) {
    Write-Warning "zkemkeeper.dll (SDK may cham cong ZKTeco) chua duoc dang ky tren may nay."
    Write-Warning "App se khong ket noi duoc may cham cong cho den khi cai SDK ZKTeco va chay 'regsvr32 zkemkeeper.dll'."
}

# 3. Stop the app if it's currently running (file lock)
Get-Process -Name "AttendanceConnect" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# 4. Copy application files, preserving an existing AttendanceConnect.exe.config on upgrade
Write-Host "Dang copy file vao $InstallDir ..."
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

$existingConfig = Join-Path $InstallDir "AttendanceConnect.exe.config"
$hasExistingConfig = Test-Path $existingConfig
if ($hasExistingConfig) {
    Copy-Item $existingConfig "$existingConfig.bak" -Force
}

Get-ChildItem -Path $sourceDir -Exclude "Setup", "AttendanceConnect.exe.config" |
    Copy-Item -Destination $InstallDir -Recurse -Force

if (-not $hasExistingConfig) {
    Copy-Item (Join-Path $sourceDir "AttendanceConnect.exe.config") $InstallDir -Force
}
else {
    Write-Host "Giu nguyen AttendanceConnect.exe.config hien co (da backup thanh .bak)" -ForegroundColor Yellow
}

# 5. Start Menu shortcut
$shortcutPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\AttendanceConnect.lnk"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $InstallDir "AttendanceConnect.exe"
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Save()

# 6. Auto-start on Windows logon (all users)
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" `
    -Name "AttendanceConnect" `
    -Value "`"$(Join-Path $InstallDir 'AttendanceConnect.exe')`""

Write-Host "Cai dat hoan tat. App se tu chay khi dang nhap Windows." -ForegroundColor Green
Write-Host "Chinh AttendanceConnect.exe.config tai: $InstallDir (hoac mo app -> tab Cai dat)" -ForegroundColor Green
