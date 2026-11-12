@echo off
cd /d "%~dp0"
echo ========================================================
echo  Building ct4cpd (PowerToys Command Palette Plugin)
echo ========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build.ps1"
pause
