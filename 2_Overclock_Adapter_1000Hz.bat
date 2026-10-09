@echo off
net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath cmd.exe -ArgumentList '/k \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)
cd /d "%~dp0"
echo Preparing 1000Hz driver files...
pushd "%~dp0hidusbf\DRIVER"
call 1kHz.cmd > nul 2>&1
popd

echo Launching HIDUSBF Setup with Administrator privileges...
echo.
echo In the HIDUSBF Setup Window:
echo  1. Click [Install Service].
echo  2. In the dropdown at the top, select 'All' (instead of Mice Only).
echo  3. Find 'WUP-028' (Hardware ID: 057E:0337).
echo  4. Check the box [Filter on Device].
echo  5. In 'Selected Rate', choose '1000' (or 1000 Hz).
echo  6. Click [Install Service] / [Restart] (or unplug and replug the adapter USB cables).
echo.
start /wait "" "%~dp0hidusbf\DRIVER\Setup.exe" /all
pause
