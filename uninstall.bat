@echo off
cd /d "%~dp0"
echo ========================================================
echo  Unregistering ct4cpd from PowerToys Command Palette
echo ========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\uninstall.ps1"
pause
