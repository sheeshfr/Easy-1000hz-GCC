<div align="center">
  <img src="logo.png" alt="Easy 1000Hz GCC" width="220" />
  <h1>Easy 1000Hz GameCube Controller Adapter Overclocker</h1>
  <p><b>1-Click 1000Hz Polling Rate • Windows 11 Memory Integrity (HVCI) Compatible • Plug & Play</b></p>
</div>

A 100% self-contained, portable, open-source toolset to configure any Nintendo Wii U or Mayflash GameCube Controller Adapter on Windows 10 & Windows 11 (x64) and overclock it to **1000 Hz** (1ms polling rate) for *Super Smash Bros. Melee* / Slippi.

* **1-Click Silent Setup:** No manual GUI clicking required—automatically installs WinUSB and binds the 1000Hz filter driver.
* **Windows 11 Memory Integrity (HVCI) Compatible:** Uses SweetLow's official Microsoft WHQL-signed `NoPatch` driver (`hidusbf.sys`) so you do **not** need to disable Core Isolation / Memory Integrity.
* **Universal Hardware Support:** Works out-of-the-box with the Official Nintendo Wii U/Switch adapter (`057E:0337`) and the Mayflash 4-Port adapter (in "Wii U" mode).
* **Fully Portable:** No hardcoded paths; run it from any folder, drive, or USB flash drive.
* **Complete Uninstaller Included:** Reverts all drivers and registry entries back to stock Windows defaults with one click.

---

## Quick Start

### 1. 1-Click Overclock (Recommended)
1. Plug your GameCube adapter into your PC:
   * **Mayflash:** Ensure the physical switch on the back is set to **"Wii U"** (not PC).
   * Ensure the **black USB cable** is plugged into a high-speed USB port.
2. Run **`Auto_Overclock_1000Hz.exe`** (or `Click_Me_To_Overclock_1000Hz.bat`).
3. Click **Yes** on the Windows UAC prompt.
4. The tool will:
   * Detect your adapter (`057E:0337`).
   * Apply the WinUSB driver.
   * Deploy the WHQL-signed 1000Hz filter driver and configure HVCI-safe parameters.
   * Re-initialize the adapter hardware.

### 2. Verify in Slippi / Dolphin
1. Open **Slippi Launcher** (or Dolphin).
2. Go to **Options -> Controller Settings** -> set **Port 1** to **GameCube Adapter for Wii U**.
3. Click **Configure** next to Port 1 to confirm the live **~1000 Hz** polling rate.
4. In Dolphin: Go to **Config -> Slippi** and check **"Reduce Timing Dispersion"**.

---

## How to Uninstall / Reset to Stock Defaults
Run **`Uninstall_and_Reset.exe`** (or `Uninstall_and_Reset.bat`).
* Stops and removes the `hidusbf` kernel service.
* Removes driver binaries from `System32\drivers\hidusbf.sys`.
* Clears all `LowerFilters` and `bInterval` values from your adapter.
* Re-initializes your adapter back to default Windows USB drivers.

---

## Package Contents & Source Code
* `AutoOverclock.cs` / `Auto_Overclock_1000Hz.exe` — The 1-click silent automation engine (C# / Win32 SetupAPI).
* `Uninstaller.cs` / `Uninstall_and_Reset.exe` — Clean uninstaller and system reset tool.
* `Verify_Adapter_Status.ps1` — Diagnostic script that checks adapter status, WinUSB binding, 1000Hz filter, and Slippi path.
* `usb_driver\` — Pre-packaged WinUSB driver package (`Orca_Controller.inf` & `.cat`).
* `hidusbf\` — Official SweetLow HIDUSBF driver suite with WHQL Microsoft-signed `NoPatch` driver for Windows 11.
* `zadig-2.9.exe` & `zadig.ini` — Open-source Zadig USB driver installer (GPLv3).
* `Run_1000Hz_Setup.bat` — Interactive step-by-step fallback wizard.

---

## Open Source Credits
* **Zadig / libwdi:** Pete Batard / Akeo ([GitHub: libwdi](https://github.com/pbatard/libwdi)) - GNU GPL v3.
* **HIDUSBF:** SweetLow / Alexander ([GitHub: hidusbf](https://github.com/LordOfMice/hidusbf)). Driver attestation by Battle Beaver Customs.
* **Project Slippi:** Jas Laferriere (Fizzi) & the Slippi team ([slippi.gg](https://slippi.gg) / [GitHub: project-slippi](https://github.com/project-slippi)) - GNU GPL v2.
