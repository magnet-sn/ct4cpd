# ct4cpd Uninstall / Unregister Script
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ct4cpd Unregister Script" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$existing = Get-AppxPackage -Name "*ct4cpd*" -ErrorAction SilentlyContinue
if ($existing) {
    foreach ($pkg in $existing) {
        Write-Host "登録解除中: $($pkg.PackageFullName)..." -ForegroundColor Yellow
        Remove-AppxPackage -Package $pkg.PackageFullName
    }
    Write-Host "`nct4cpd のパッケージ登録を解除しました。" -ForegroundColor Green
} else {
    Write-Host "登録されている ct4cpd パッケージは見つかりませんでした。" -ForegroundColor Yellow
}
