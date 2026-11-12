# ct4cpd Easy Install & Register Script
# Run with PowerShell (Normal User or Administrator)

[CmdletBinding()]
param(
    [string]$TargetDir = ""
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ct4cpd Installer for Command Palette" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. AppxManifest.xml の場所を探索
$manifestCandidates = @()

if ($TargetDir -and (Test-Path $TargetDir)) {
    $manifestCandidates += (Join-Path $TargetDir "AppxManifest.xml")
}

# スクリプトと同じディレクトリ
$manifestCandidates += (Join-Path $PSScriptRoot "AppxManifest.xml")

# ビルド出力ディレクトリ (x64 Release)
$manifestCandidates += (Join-Path $PSScriptRoot "..\ct4cpd\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml")
$manifestCandidates += (Join-Path $PSScriptRoot "..\ct4cpd\bin\Release\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml")

# Program Files インストール先
$manifestCandidates += "C:\Program Files\ct4cpd\AppxManifest.xml"

$manifestPath = $null
foreach ($path in $manifestCandidates) {
    if (Test-Path $path) {
        $manifestPath = (Resolve-Path $path).Path
        break
    }
}

if (-not $manifestPath) {
    Write-Error "AppxManifest.xml が見つかりませんでした。先に scripts\build.ps1 でビルドを実行してください。"
    exit 1
}

Write-Host "対象マニフェスト: $manifestPath" -ForegroundColor Yellow

# 2. 既存パッケージの登録解除
Write-Host "`n[1/2] 既存の ct4cpd パッケージを確認中..."
$existing = Get-AppxPackage -Name "*ct4cpd*" -ErrorAction SilentlyContinue
if ($existing) {
    foreach ($pkg in $existing) {
        Write-Host "  古いパッケージを登録解除中: $($pkg.PackageFullName)" -ForegroundColor DarkGray
        Remove-AppxPackage -Package $pkg.PackageFullName
    }
} else {
    Write-Host "  既存のパッケージはありません。" -ForegroundColor DarkGray
}

# 3. 新しいパッケージの登録
Write-Host "`n[2/2] ct4cpd を Command Palette 拡張機能として登録中..."
try {
    Add-AppxPackage -Register $manifestPath -ForceApplicationShutdown
    Write-Host "`n==========================================" -ForegroundColor Green
    Write-Host " インストール / 登録が正常に完了しました！" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "※ PowerToys Command Palette のドックで CPU 温度が表示されることを確認してください。"
    Write-Host "※ ドックの縦置き/横置き設定は、Command Palette の拡張機能設定から変更できます。"
} catch {
    Write-Error "パッケージの登録に失敗しました: $_"
    exit 1
}
