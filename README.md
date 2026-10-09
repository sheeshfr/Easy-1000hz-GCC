<div align="center">

  <img src="logo.png" alt="Easy 1000Hz GCC Logo" width="180" />

  # Easy 1000Hz GCC

  **The 1-Click 1000Hz Adapter Overclocker for Slippi (the definitive way to play Super Smash Bros. Melee on unofficial hardware (new official tbh?? 🤔))**

  [![Download Release](https://img.shields.io/badge/Download-Latest%20Release%20(.ZIP)-brightgreen?style=for-the-badge&logo=github)](https://github.com/sheeshfr/Easy-1000hz-GCC/releases/latest)
  [![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/sheeshfr/Easy-1000hz-GCC)
  [![License](https://img.shields.io/badge/License-GLP--1-blueviolet?style=for-the-badge)](https://www.youtube.com/watch?v=xsfcbTRNxyI&list=RDxsfcbTRNxyI&start_radio=1)

</div>

---

A simple 1-click tool that overclocks your GameCube controller adapter to **1000Hz (1ms response time)** for playing *Super Smash Bros. Melee* on Slippi.

### Why do this?
Standard GameCube adapters only check for your controller inputs every 8 milliseconds. Overclocking drops that down to **1 millisecond**, making your inputs feel instant and crisp.

Best of all: it is **100% plug-and-play** and works safely on both Windows 10 & 11 without touching your PC's security settings.

## 🎮 Supported Hardware & Controllers

Works out of the box with standard adapters and next-gen controllers:

* 🐋 **[The Orca Analog Controller](https://theorca.gg):** Fully supported! The Orca is the holy grail of modern *Melee* controllers—a gorgeous stickless box engineered with **magnetic Hall Effect switches** for true analog depth (real analog stick angles, drift control, and shield drops in an ergonomic leverless format). This tool sets up your Orca at blistering 1000Hz (1ms) speed natively.
* **Official Nintendo Wii U Adapter**
* **Official Nintendo Switch Adapter**
* **Mayflash 4-Port Adapter** *(make sure the switch on the back is set to "Wii U")*
* **Mayflash 2-Port Adapter** *(set to "Wii U")*
* **Generic 4-Port Clone Adapters**

---

## 🚀 How to Use (3 Simple Steps)

### Step 1: Plug in your adapter / controller
* **Standard Adapters:** Plug the **black USB cable** into your PC (the grey cable is only needed for rumble). If you have a **Mayflash adapter**, make sure the switch on the back is set to **"Wii U"** (not PC).
* **The Orca:** Simply plug in your USB-C cable directly.

### Step 2: Run the installer
* Double-click **`Auto_Overclock_1000Hz.exe`** (or `Click_Me_To_Overclock_1000Hz.bat`).
* Click **Yes** when Windows asks for permission.
* It sets up everything automatically in about two seconds.

### Step 3: Open Slippi and play!
* Open **Slippi** (or Dolphin).
* Go to **Options** ➔ **Controller Settings**.
* Set **Port 1** to **GameCube Adapter for Wii U** and click **Configure**.
* You should see **Adapter Detected** and **~1000Hz**!

*(Pro tip: In Dolphin under **Config** ➔ **Slippi**, make sure **"Reduce Timing Dispersion"** is checked for the smoothest inputs).*

---

## 🔄 How to Uninstall / Reset

If you ever want to reset your adapter back to factory default Windows settings:

1. Run **`Uninstall_and_Reset.exe`** (or `Uninstall_and_Reset.bat`).
2. Click **Yes** on the prompt.
3. Everything is cleanly removed and restored to normal.

---

## ❓ Frequently Asked Questions

**Q: Slippi / Dolphin says "0 adapters detected"?**  
A: Make sure the **black USB plug** is connected. If you have a Mayflash adapter, check that the switch on the back is set to **Wii U**, not PC. Then run `Auto_Overclock_1000Hz.exe` again.

**Q: The number shows 990Hz or 1005Hz instead of exactly 1000Hz?**  
A: That's completely normal! USB timing fluctuates slightly. As long as it's hovering near ~1000Hz, you are getting full 1ms speed.

**Q: Do I need to run this every time I turn on my computer?**  
A: Nope! You only run it once. The settings stay permanently saved. *(If you ever plug the adapter into a brand-new USB port on your PC, just run it once for that port).*

**Q: Windows Defender / SmartScreen says "Windows protected your PC"?**  
A: Click **More info**, then click **Run anyway**. This is just Windows warning you about a newly downloaded file.

---

## 📖 Nerdy Technical Details

Curious about how the driver injection, registry entries, and Windows kernel signatures work under the hood? Read the full technical guide here:

👉 **[DOCUMENTATION.md](DOCUMENTATION.md)**

---

## 📜 Credits

* **SweetLow:** Creator of the HIDUSBF overclocking driver.
* **Battle Beaver Customs:** For driver certification on modern Windows.
* **Pete Batard:** Creator of Zadig / libwdi.
* **Fizzi & the Slippi Team:** For making modern rollback Melee possible.

<div align="center">
  <sub>Made for the Melee & Slippi community with ❤️</sub>
</div>
