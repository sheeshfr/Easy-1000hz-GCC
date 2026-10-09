# Technical Documentation & Architecture Guide

Comprehensive technical documentation for the **Easy 1000Hz GameCube Controller Adapter Overclocker**.

---

## Table of Contents
1. [Executive Summary](#1-executive-summary)
2. [Background & Latency Mechanics](#2-background--latency-mechanics)
   - [The 125 Hz Bottleneck](#the-125-hz-bottleneck)
   - [The Windows 11 HVCI Challenge](#the-windows-11-hvci-challenge)
3. [Architecture & System Flow](#3-architecture--system-flow)
   - [Component Overview](#component-overview)
   - [Automated Pipeline Steps](#automated-pipeline-steps)
4. [Driver & Kernel Subsystem Details](#4-driver--kernel-subsystem-details)
   - [WinUSB Device Driver Binding](#winusb-device-driver-binding)
   - [SweetLow HIDUSBF Filter Driver](#sweetlow-hidusbf-filter-driver)
   - [Microsoft WHQL Attestation & HVCI Compliance](#microsoft-whql-attestation--hvci-compliance)
5. [Hardware Registry Internals](#5-hardware-registry-internals)
   - [Registry Path & Keys](#registry-path--keys)
   - [LowerFilters Injection](#lowerfilters-injection)
   - [Parameter Definitions](#parameter-definitions)
6. [Hardware Re-enumeration (No Reboot Required)](#6-hardware-re-enumeration-no-reboot-required)
7. [Dolphin & Project Slippi Integration](#7-dolphin--project-slippi-integration)
   - [Native Wii U Adapter Mode](#native-wii-u-adapter-mode)
   - [Reduce Timing Dispersion Setting](#reduce-timing-dispersion-setting)
8. [Uninstallation & Rollback Mechanism](#8-uninstallation--rollback-mechanism)
9. [Build Instructions & Development](#9-build-instructions--development)
10. [References & Attributions](#10-references--attributions)

---

## 1. Executive Summary

Standard USB GameCube Controller adapters (Nintendo Wii U / Switch official `057E:0337` and Mayflash 4-port adapters) report to the operating system at a default USB interrupt polling rate of **125 Hz** (an interval of **8 milliseconds**). In competitive *Super Smash Bros. Melee* emulation (via Project Slippi / Dolphin), this 8ms polling interval introduces variable input latency and frame-alignment jitter.

This toolkit provides an automated, portable, and non-destructive solution to overclock the adapter's polling rate to **1000 Hz** (**1 millisecond** polling interval), reducing maximum input latency by up to **7.0 ms**. 

Crucially, unlike legacy overclocking methods that require users to disable Windows 11 **Memory Integrity (HVCI / Core Isolation)**, this implementation uses a **Microsoft WHQL-certified, attestation-signed kernel filter driver (`hidusbf.sys`)** configured in strict `NoPatch` mode, maintaining full operating system security.

---

## 2. Background & Latency Mechanics

### The 125 Hz Bottleneck
*Super Smash Bros. Melee* renders and evaluates game logic at **60 frames per second** (~16.67 milliseconds per frame). 

```
Standard Polling (125 Hz / 8 ms Interval):
Frame Window (16.6ms): [   Poll 1 (8ms)   |   Poll 2 (8ms)   ]
Input Delay Variance: Up to 8 ms delay depending on when a physical button press occurs relative to the poll cycle.

Overclocked Polling (1000 Hz / 1 ms Interval):
Frame Window (16.6ms): [||||||||||||||||] (16 discrete polls per frame)
Input Delay Variance: ≤ 1 ms delay.
```

By increasing the polling frequency to 1000 Hz:
- Controller inputs are queried every 1 millisecond.
- Input data arrives in Dolphin / Slippi memory within 1 ms of physical stick movement or button depression.
- Combined with Slippi's **Reduce Timing Dispersion** algorithm, polling jitter is effectively eliminated.

### The Windows 11 HVCI Challenge
Historically, the Smash community relied on manual installation of SweetLow's `hidusbf` using tools like `Setup.exe` that patched USB system drivers (`usbport.sys` / `usbehci.sys` / `usbxhci.sys`). 

Starting in Windows 10 (20H1+) and standard on Windows 11, Microsoft enabled **Hypervisor-Protected Code Integrity (HVCI)**, commonly known as **Memory Integrity** or **Core Isolation**. HVCI strictly prevents:
1. Loading kernel drivers with modified, unverified code tables or self-signed certificates.
2. In-memory patching of system binaries like the USB Host Controller stack.

If a user attempted legacy overclocking guides on modern Windows 11, the operating system would either block driver installation with error code 39 / 52, or trigger a Kernel Security Check Failure Blue Screen (BSOD).

**The Solution:**
This project utilizes the signed `hidusbf.sys` driver (`AMD64_AS/NoPatch`), which carries a valid Microsoft WHQL Hardware Compatibility signature. It sets `PatchUSBPort = 0` and `PatchUSBXHCI = 0`, acting exclusively as a legitimate WDM device lower filter. This enables 1000 Hz polling while keeping Windows Memory Integrity completely active.

---

## 3. Architecture & System Flow

### Component Overview
The project is built around a lightweight native C# automation engine ([`AutoOverclock.cs`](file:///r:/AI%20Coding/_Antigravity/Projects/Conversations%20w%20AI/GC_Adapter_Overclock/AutoOverclock.cs)) compiled directly with the .NET Framework `csc.exe`, requiring zero external runtimes or dependencies.

```
+-------------------------------------------------------------------------+
|                        Auto_Overclock_1000Hz.exe                        |
|                         (Manifest: requireAdmin)                        |
+--------------------+-------------------------------+--------------------+
                     |                               |
       1. WinUSB Device Driver          2. HIDUSBF Filter Driver
       -----------------------          ------------------------
       Locates 057E:0337 hardware       Installs hidusbf kernel service
       Installs Orca_Controller.inf     Copies AMD64_AS/NoPatch/hidusbf.sys
       Binds WinUSB via SetupAPI        Injects LowerFilters into Device Key
                     \                               /
                      \                             /
                   +---v---------------------------v---+
                   |     CM_Query_And_Remove_SubTree   |
                   |      CM_Reenumerate_DevNode       |
                   |   (Live Device Cycle, No Reboot)  |
                   +-----------------+-----------------+
                                     |
                                     v
                   +-----------------------------------+
                   |     Adapter Polling at 1000Hz     |
                   |   bInterval = 1 (1ms Latency)     |
                   +-----------------------------------+
```

### Automated Pipeline Steps
1. **Privilege & Environment Verification:**
   Verifies 64-bit architecture and elevated Administrator privileges via Windows UAC.
2. **Hardware Discovery:**
   Enumerates USB device trees using Windows SetupAPI (`SetupDiGetClassDevs`) to locate hardware matching `VID_057E&PID_0337`.
3. **WinUSB Installation:**
   Applies `usb_driver\Orca_Controller.inf` to configure the adapter with the Microsoft WinUSB stack, required for direct controller access in Dolphin.
4. **Driver Service Registration:**
   Registers the `hidusbf` kernel service in `HKLM\SYSTEM\CurrentControlSet\Services\hidusbf` and deploys the certified 64-bit binary to `C:\Windows\System32\drivers\hidusbf.sys`.
5. **Registry Filter Injection:**
   Modifies the specific USB device instance hardware key in `HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_057E&PID_0337\...`:
   - Adds `hidusbf` as a `REG_MULTI_SZ` entry under `LowerFilters`.
   - Writes `bInterval = 1` (`REG_DWORD`).
   - Writes `PatchUSBPort = 0` and `PatchUSBXHCI = 0` (`REG_DWORD`).
6. **Device Re-enumeration:**
   Calls Windows Configuration Manager (`cfgmgr32.dll`) to restart the USB device node dynamically. The adapter immediately loads the new driver stack without a PC reboot.

---

## 4. Driver & Kernel Subsystem Details

### WinUSB Device Driver Binding
Nintendo's adapter is not a standard HID gamepad; it is an asynchronous bulk transfer USB interface. Dolphin / Slippi communicates directly through the **WinUSB** kernel interface (`winusb.sys`).
The driver package bundled in `usb_driver/` contains:
- `Orca_Controller.inf`: The device setup information configuring class GUID `{88BAE032-5A81-49f0-BC3D-A4FF138216D6}`.
- `Orca_Controller.cat`: Cryptographic catalog containing the digital signature.
- `amd64\winusbcoinstaller2.dll` and `WdfCoInstaller01011.dll`: Standard Microsoft WDF co-installers.

### SweetLow HIDUSBF Filter Driver
SweetLow's `hidusbf` operates as an Upper or Lower Device Filter in the Windows Driver Model (WDM):
- When attached to the USB device stack, `hidusbf.sys` intercepts `URB_FUNCTION_SELECT_CONFIGURATION` and interrupt endpoint descriptors during adapter initialization.
- It dynamically rewires the device's polling endpoint interval byte (`bInterval`) to `1` (which translates to 1 ms on Full-Speed USB, or 125 microseconds on High-Speed USB).

### Microsoft WHQL Attestation & HVCI Compliance
The driver binary deployed is located at:
`hidusbf\DRIVER\AMD64_AS\NoPatch\hidusbf.sys`
- **Catalog Certificate:** Attestation-signed by the **Microsoft Windows Hardware Compatibility Publisher**.
- **Cross-Sign Authority:** Trusted by the Windows Kernel Root Certificate Authority.
- **HVCI Flag:** Strictly avoids mapping non-executable memory as executable and does not perform unhooked kernel memory writes.

---

## 5. Hardware Registry Internals

### Registry Path & Keys
All adapter-specific settings are applied directly to the hardware device registry instance:
```
HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_057E&PID_0337\<DeviceInstanceID>
```

### Key Values

| Registry Name | Type | Value | Function |
| :--- | :--- | :--- | :--- |
| `LowerFilters` | `REG_MULTI_SZ` | `hidusbf` | Injects the filter driver directly beneath the primary function driver. |
| `bInterval` | `REG_DWORD` | `1` | Forces polling interval to 1 millisecond (1000 Hz). |
| `PatchUSBPort` | `REG_DWORD` | `0` | Disables USB 2.0 port driver patching (required for HVCI). |
| `PatchUSBXHCI` | `REG_DWORD` | `0` | Disables USB 3.x xHCI driver patching (required for HVCI). |

### Service Key
The driver service registration resides at:
```
HKLM\SYSTEM\CurrentControlSet\Services\hidusbf
```
- `DisplayName` = `"HID USB Filter"`
- `ErrorControl` = `1` (Normal)
- `ImagePath` = `\SystemRoot\System32\drivers\hidusbf.sys`
- `Start` = `3` (SERVICE_DEMAND_START)
- `Type` = `1` (SERVICE_KERNEL_DRIVER)

---

## 6. Hardware Re-enumeration (No Reboot Required)

In standard Windows driver modification workflows, users are prompted to restart their computer or physically disconnect and reconnect their USB cables. 

To achieve a true **1-click silent experience**, [`AutoOverclock.cs`](file:///r:/AI%20Coding/_Antigravity/Projects/Conversations%20w%20AI/GC_Adapter_Overclock/AutoOverclock.cs) imports `cfgmgr32.dll` and invokes two low-level Configuration Manager APIs:

```csharp
[DllImport("cfgmgr32.dll", SetLastError = true)]
internal static extern int CM_Query_And_Remove_SubTree(
    uint dnDevInst,
    out IntPtr pVetoType,
    StringBuilder pszVetoName,
    int ulNameLength,
    int ulFlags);

[DllImport("cfgmgr32.dll", SetLastError = true)]
internal static extern int CM_Reenumerate_DevNode(
    uint dnDevInst,
    int ulFlags);
```

1. `CM_Query_And_Remove_SubTree` safely stops and detaches the USB adapter device node without interrupting other devices on the same USB Root Hub.
2. `CM_Reenumerate_DevNode` forces the parent USB Hub to re-detect downstream devices.
3. Windows queries the newly written `LowerFilters` and `bInterval` registry entries and launches the 1000 Hz driver stack immediately.

---

## 7. Dolphin & Project Slippi Integration

### Native Wii U Adapter Mode
Project Slippi (and mainline Dolphin) implements direct raw USB communication with the GameCube adapter:
1. In Slippi Dolphin, navigate to **Options -> Controller Settings**.
2. Under **Port 1**, select **GameCube Adapter for Wii U**.
3. Slippi connects via WinUSB using asynchronous transfers.
4. Click **Configure** on Port 1 to view live diagnostics:
   - Polling rate counter will display **~1000 Hz** (or 990 - 1010 Hz).
   - Adapter status will indicate **Adapter Detected**.

### Reduce Timing Dispersion Setting
Located under **Config -> Slippi**:
- **"Reduce Timing Dispersion"** synchronizes Dolphin's controller read thread with the adapter's incoming polling packets.
- When paired with a 1000 Hz polling rate, input jitter is reduced to sub-millisecond precision, providing the closest possible timing accuracy to CRT console hardware.

---

## 8. Uninstallation & Rollback Mechanism

The [`Uninstaller.cs`](file:///r:/AI%20Coding/_Antigravity/Projects/Conversations%20w%20AI/GC_Adapter_Overclock/Uninstaller.cs) utility restores the host machine to pristine factory conditions:

1. **Stops Kernel Service:** Stops any running instances of the `hidusbf` service via Windows Service Control Manager (`Advapi32.dll`).
2. **Deletes Service Registration:** Removes `HKLM\SYSTEM\CurrentControlSet\Services\hidusbf`.
3. **Prunes Registry Filter Keys:**
   - Traverses `HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_057E&PID_0337`.
   - Removes `hidusbf` from `LowerFilters`. If `LowerFilters` is empty, deletes the entry.
   - Deletes `bInterval`, `PatchUSBPort`, and `PatchUSBXHCI`.
4. **Removes Driver Binary:** Deletes `C:\Windows\System32\drivers\hidusbf.sys`.
5. **Re-enumerates Adapter:** Soft-cycles the hardware to return the adapter to default 125 Hz operation.

---

## 9. Build Instructions & Development

The source binaries can be recompiled on any Windows machine with the .NET Framework 4.5+ installed (included by default on Windows 10 and 11).

### Compiling `Auto_Overclock_1000Hz.exe`
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
    /target:winexe `
    /win32manifest:app.manifest `
    /out:Auto_Overclock_1000Hz.exe `
    /optimize+ `
    AutoOverclock.cs
```

### Compiling `Uninstall_and_Reset.exe`
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
    /target:winexe `
    /win32manifest:app.manifest `
    /out:Uninstall_and_Reset.exe `
    /optimize+ `
    Uninstaller.cs
```

The application manifest (`app.manifest`) specifies `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />` to ensure Windows displays standard UAC elevation prompts rather than failing silently on protected registry operations.

---

## 10. References & Attributions

- **SweetLow (Alexander):** Creator of the HIDUSBF USB polling rate overclocking filter driver. ([GitHub Repository](https://github.com/LordOfMice/hidusbf)).
- **Battle Beaver Customs:** Attestation and code-signing facilitation for WHQL compatibility.
- **Pete Batard / Akeo:** Author of Zadig and `libwdi` driver installer tools. ([GitHub Repository](https://github.com/pbatard/libwdi)).
- **Project Slippi:** Jas Laferriere (Fizzi) and the Slippi development team for netplay and rollback emulation. ([slippi.gg](https://slippi.gg)).
