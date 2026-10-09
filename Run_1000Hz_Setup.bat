@echo off
:: =======================================================================
:: Auto-Elevation Check: Ensure script runs with Administrator rights
:: =======================================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [INFO] Requesting Administrator privileges to manage USB drivers...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath cmd.exe -ArgumentList '/k \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

setlocal enabledelayedexpansion
cd /d "%~dp0"
title GameCube Adapter 1000Hz Overclock Setup

cls
echo =====================================================================
echo          GameCube Controller Adapter 1000Hz Overclock Setup
echo =====================================================================
echo.
echo This portable package contains all necessary drivers and tools to:
echo   1. Replace default driver with WinUSB (Zadig)
echo   2. Overclock USB polling to 1000Hz (SweetLow HIDUSBF)
echo   3. Verify hardware status and polling rate
echo.
echo Requirements:
echo   - If using a Mayflash adapter, switch on back must be set to 'Wii U'.
echo   - Black USB plug must be connected (connect to rear motherboard port).
echo =====================================================================
echo.
pause

:: =======================================================================
:: STEP 1: ZADIG (WinUSB Installation)
:: =======================================================================
cls
echo =====================================================================
echo  STEP 1 of 2: Install WinUSB Driver (Zadig)
echo =====================================================================
echo.
echo Launching Zadig now...
echo.
echo IN THE ZADIG WINDOW:
echo   1. Ensure 'WUP-028' (USB ID: 057E 0337) is selected in the dropdown.
echo   2. Ensure the target driver on the right is 'WinUSB'.
echo   3. Click 'Replace Driver' (or 'Install Driver').
echo   4. Once complete, close Zadig to proceed.
echo.
echo =====================================================================

start /wait "" "%~dp0zadig-2.9.exe"

echo.
echo Checking adapter driver status...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$dev = Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -match '057E.*0337' }; if ($dev -and $dev.Status -eq 'OK') { Write-Host '[SUCCESS] Adapter is active and using WinUSB!' -ForegroundColor Green } else { Write-Host '[NOTE] If driver did not finish, you can rerun this step anytime.' -ForegroundColor Yellow }"

echo.
pause

:: =======================================================================
:: STEP 2: HIDUSBF (1000Hz Overclock)
:: =======================================================================
cls
echo =====================================================================
echo  STEP 2 of 2: Apply 1000Hz Polling Overclock (HIDUSBF)
echo =====================================================================
echo.
echo Preparing 1000Hz driver profile...
pushd "%~dp0hidusbf\DRIVER"
call 1kHz.cmd > nul 2>&1
popd

echo Launching HIDUSBF Setup...
echo.
echo IN THE HIDUSBF SETUP WINDOW:
echo   1. Click the [Install Service] button.
echo   2. In the dropdown list at the top, switch from 'Mice Only' to 'All'.
echo   3. Find and click the row for 'WUP-028' (Hardware ID: 057E:0337).
echo   4. Check the box [Filter on Device].
echo   5. In 'Selected Rate', choose '1000' (1000 Hz).
echo   6. Click [Install Service] again or [Restart] (or unplug and replug the adapter).
echo   7. Close the window when done.
echo.
echo =====================================================================

start /wait "" "%~dp0hidusbf\DRIVER\Setup.exe" /all

:: =======================================================================
:: STEP 3: AUTOMATIC VERIFICATION
:: =======================================================================
cls
echo =====================================================================
echo                     FINAL STATUS VERIFICATION
echo =====================================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify_Adapter_Status.ps1"
echo.
echo =====================================================================
echo Next Steps in Slippi Dolphin:
echo   1. Open Slippi Launcher -> Launch Dolphin.
echo   2. In Dolphin: 'Options' -> 'Controller Settings' -> set Port 1 to 'GameCube Adapter for Wii U'.
echo   3. Click 'Configure' next to Port 1 to view live polling rate (~1000Hz).
echo   4. In Dolphin: 'Config' -> 'Slippi' -> enable 'Reduce Timing Dispersion'.
echo =====================================================================
echo.
pause
