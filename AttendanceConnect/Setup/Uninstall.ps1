#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstalls AttendanceConnect from this machine.
#>
param(
    [string]$InstallDir = "$env:ProgramFiles\AttendanceConnect"
)

$ErrorActionPreference = "Stop"

Write-Host "=== AttendanceConnect Uninstall ===" -ForegroundColor Cyan

Get-Process -Name "AttendanceConnect" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Remove-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "AttendanceConnect" -ErrorAction SilentlyContinue

Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\AttendanceConnect.lnk" -ErrorAction SilentlyContinue

if (-not (Test-Path $InstallDir)) {
    Write-Host "Khong tim thay $InstallDir - co the da go truoc do." -ForegroundColor Yellow
    exit 0
}

$keepData = Read-Host "Giu lai AttendanceConnect.exe.config va logs? (y/n)"
if ($keepData -eq "y") {
    Get-ChildItem -Path $InstallDir -Exclude "AttendanceConnect.exe.config", "AttendanceConnect.exe.config.bak", "logs", "last_sync.txt" |
        Remove-Item -Recurse -Force
    Write-Host "Da go AttendanceConnect, giu lai AttendanceConnect.exe.config va logs tai $InstallDir" -ForegroundColor Green
}
else {
    Remove-Item -Path $InstallDir -Recurse -Force
    Write-Host "Da go AttendanceConnect hoan toan." -ForegroundColor Green
}
