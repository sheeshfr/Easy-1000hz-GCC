@echo off
net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath cmd.exe -ArgumentList '/k \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)
cd /d "%~dp0"
echo Launching Zadig with Administrator privileges...
echo.
echo In Zadig:
echo  1. Select 'WUP-028' (ID: 057E 0337) in the dropdown.
echo  2. Ensure the target driver is set to WinUSB.
echo  3. Click 'Replace Driver' or 'Install Driver'.
echo  4. Once finished, close Zadig.
echo.
start /wait "" "%~dp0zadig-2.9.exe"
pause
