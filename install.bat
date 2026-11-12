@echo off
cd /d "%~dp0"
echo ========================================================
echo  Installing ct4cpd to PowerToys Command Palette
echo ========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\install.ps1"
pause
