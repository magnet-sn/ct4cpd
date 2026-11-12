# ct4cpd Build Script
[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("x64", "ARM64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$projectDir = Resolve-Path (Join-Path $PSScriptRoot "..\ct4cpd")
$csprojPath = Join-Path $projectDir "ct4cpd.csproj"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building ct4cpd [$Configuration | $Platform]" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

dotnet build $csprojPath -c $Configuration -p:Platform=$Platform

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
