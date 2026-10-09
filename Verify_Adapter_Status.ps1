# Portable GameCube Adapter and 1000Hz Overclock Status Verifier
Write-Host "=== Checking GameCube Controller Adapter Status ===" -ForegroundColor Cyan

$device = Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -match '057E.*0337' -or $_.FriendlyName -match 'WUP-028' }

if (-not $device) {
    Write-Host "[!] GameCube Adapter (057E:0337) not detected! Please ensure it is plugged into a USB port." -ForegroundColor Red
} else {
    Write-Host "[+] Found Adapter Device:" -ForegroundColor Green
    Write-Host "    Name:   $($device.FriendlyName)"
    Write-Host "    Status: $($device.Status)"
    Write-Host "    ID:     $($device.InstanceId)"
    
    # Query active driver details
    $driverProp = Get-PnpDeviceProperty -InstanceId $device.InstanceId -ErrorAction SilentlyContinue | Where-Object { $_.KeyName -eq 'DEVPKEY_Device_DriverDesc' }
    $driverProvider = Get-PnpDeviceProperty -InstanceId $device.InstanceId -ErrorAction SilentlyContinue | Where-Object { $_.KeyName -eq 'DEVPKEY_Device_DriverProvider' }
    $driverInf = Get-PnpDeviceProperty -InstanceId $device.InstanceId -ErrorAction SilentlyContinue | Where-Object { $_.KeyName -eq 'DEVPKEY_Device_DriverInfPath' }
    
    Write-Host "    Driver: $($driverProp.Data) ($($driverProvider.Data)) - INF: $($driverInf.Data)"

    if ($driverInf.Data -eq 'input.inf') {
        Write-Host "    [!] Adapter is currently using default Windows HID driver (input.inf)." -ForegroundColor Yellow
        Write-Host "        -> WinUSB driver must be installed using Zadig (Step 1)." -ForegroundColor Yellow
    } elseif ($driverProp.Data -match 'WinUSB' -or $driverInf.Data -match 'oem.*\.inf') {
        Write-Host "    [OK] WinUSB driver is active on the adapter!" -ForegroundColor Green
    }

    # Query LowerFilters registration in registry
    $instancePath = $device.InstanceId -replace '\\', '\'
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$instancePath"
    $lowerFilters = (Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue).LowerFilters
    if ($lowerFilters -contains 'hidusbf') {
        Write-Host "    [OK] HIDUSBF filter is registered on adapter (LowerFilters contains hidusbf)!" -ForegroundColor Green
    } else {
        Write-Host "    [!] HIDUSBF filter is NOT yet registered on adapter (perform Step 2)." -ForegroundColor Yellow
    }

    $bInt = (Get-ItemProperty -Path "$regPath\Device Parameters" -ErrorAction SilentlyContinue).bInterval
    if ($bInt -eq 1) {
        Write-Host "    [OK] Polling rate bInterval configured to: 1 (1000Hz / 1ms polling rate)!" -ForegroundColor Green
    } elseif ($null -ne $bInt) {
        Write-Host "    [*] Polling rate bInterval configured to: $bInt" -ForegroundColor Cyan
    }
}

Write-Host "`n=== Checking HIDUSBF Driver Service ===" -ForegroundColor Cyan
$service = Get-Service -Name "hidusbf" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "[+] HIDUSBF Service Status: $($service.Status)" -ForegroundColor Green
} else {
    Write-Host "[!] HIDUSBF Service is not currently installed." -ForegroundColor Yellow
}

Write-Host "`n=== Checking Slippi Installation ===" -ForegroundColor Cyan
$possibleSlippiLocations = @(
    "$env:LOCALAPPDATA\Programs\Slippi Launcher\Slippi Launcher.exe",
    "$env:APPDATA\Slippi Launcher\Slippi Launcher.exe",
    "$env:ProgramFiles\Slippi Launcher\Slippi Launcher.exe",
    "${env:ProgramFiles(x86)}\Slippi Launcher\Slippi Launcher.exe"
)

$foundSlippi = $null
foreach ($path in $possibleSlippiLocations) {
    if (Test-Path $path) {
        $foundSlippi = $path
        break
    }
}

if ($foundSlippi) {
    Write-Host "[OK] Slippi Launcher detected at: $foundSlippi" -ForegroundColor Green
} else {
    $slippiProc = Get-Process -Name "*slippi*" -ErrorAction SilentlyContinue
    if ($slippiProc) {
        Write-Host "[OK] Slippi Launcher is currently running (Process ID: $($slippiProc[0].Id))" -ForegroundColor Green
    } else {
        Write-Host "[!] Slippi Launcher executable not detected in default paths (can be installed anytime from slippi.gg)." -ForegroundColor Yellow
    }
}
