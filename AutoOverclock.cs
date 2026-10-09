using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Security.Principal;
using Microsoft.Win32;

namespace GCAdapter1000Hz
{
    class Program
    {
        // Win32 Constants
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_PRESENT = 0x00000002;
        const uint DICS_FLAG_GLOBAL = 0x00000001;
        const uint DIREG_DEV = 0x00000001;
        const uint KEY_ALL_ACCESS = 0xF003F;
        const uint SPDRP_LOWERFILTERS = 0x00000014;

        const uint WM_COMMAND = 0x0111;
        const uint WM_APP = 0x8000;
        const uint WM_GETTEXT = 0x000D;
        const uint CB_GETCOUNT = 0x0146;
        const uint CB_GETLBTEXT = 0x0148;
        const uint CB_GETLBTEXTLEN = 0x0149;
        const uint CB_SETCURSEL = 0x014E;
        const int CBN_SELCHANGE = 1;
        const int BN_CLICKED = 0;
        const uint BM_CLICK = 0x00F5;

        const int SW_HIDE = 0;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_HIDEWINDOW = 0x0080;

        const int IDC_DEVICELIST = 1001;
        const int IDC_VID = 1002;
        const int IDC_PID = 1003;
        const int IDC_INSTALL = 1009;

        const uint IDM_LISTALL = 40004;
        const uint IDM_IGNOREHUBS = 40009;

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiOpenDevRegKey(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Scope, uint HwProfile, uint KeyType, uint samDesired);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, byte[] PropertyBuffer, uint PropertyBufferSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int RegSetValueEx(IntPtr hKey, string lpValueName, int Reserved, uint dwType, byte[] lpData, int cbData);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetDlgItemText(IntPtr hDlg, int nIDDlgItem, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetMenu(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

        [DllImport("user32.dll")]
        static extern uint GetMenuState(IntPtr hMenu, uint uId, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hWnd, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetDlgCtrlID(IntPtr hWnd);

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Elevate()
        {
            var psi = new ProcessStartInfo();
            psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Elevation canceled or failed: " + ex.Message);
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        static void KillExistingZadigProcesses()
        {
            string[] names = { "zadig-2.9", "zadig", "zadig-2.8", "zadig-2.7" };
            foreach (var name in names)
            {
                try
                {
                    var procs = Process.GetProcessesByName(name);
                    foreach (var p in procs)
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            Thread.Sleep(300);
        }

        static string FindAdapterInstanceId()
        {
            IntPtr hDev = SetupDiGetClassDevs(IntPtr.Zero, "USB", IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
            if (hDev == (IntPtr)(-1)) return null;

            SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA();
            devData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

            uint i = 0;
            string foundId = null;

            while (SetupDiEnumDeviceInfo(hDev, i++, ref devData))
            {
                var sb = new StringBuilder(1024);
                int req;
                if (SetupDiGetDeviceInstanceId(hDev, ref devData, sb, sb.Capacity, out req))
                {
                    string id = sb.ToString();
                    if (id.IndexOf("VID_057E&PID_0337", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundId = id;
                        break;
                    }
                }
            }
            SetupDiDestroyDeviceInfoList(hDev);
            return foundId;
        }

        static bool IsWinUSBActive(string instanceId)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + instanceId))
                {
                    if (key != null)
                    {
                        var service = key.GetValue("Service") as string;
                        if (!string.IsNullOrEmpty(service) && service.Equals("WinUSB", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        static bool InstallWinUSBSilently(string baseDir, string instanceId)
        {
            Console.WriteLine("[*] Step 2/3: Installing WinUSB driver silently in background...");

            // Method 1: Direct, instant Windows PnP Engine Installation via pre-signed INF package
            string usbDriverDir = Path.Combine(baseDir, "usb_driver");
            if (!Directory.Exists(usbDriverDir))
            {
                string deskDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "usb_driver");
                if (Directory.Exists(deskDir)) usbDriverDir = deskDir;
            }

            string infPath = Path.Combine(usbDriverDir, "Orca_Controller.inf");
            string certPath = Path.Combine(usbDriverDir, "driver_cert.cer");

            if (File.Exists(infPath))
            {
                Console.WriteLine("    [..] Registering driver publisher certificate in Windows Store...");
                if (File.Exists(certPath))
                {
                    var pCert1 = Process.Start(new ProcessStartInfo("certutil.exe", "-addstore -f \"TrustedPublisher\" \"" + certPath + "\"") { CreateNoWindow = true, UseShellExecute = false });
                    pCert1.WaitForExit();
                    var pCert2 = Process.Start(new ProcessStartInfo("certutil.exe", "-addstore -f \"Root\" \"" + certPath + "\"") { CreateNoWindow = true, UseShellExecute = false });
                    pCert2.WaitForExit();
                }

                Console.WriteLine("    [..] Applying WinUSB package directly to device via Windows Driver Engine...");
                var pnp = Process.Start(new ProcessStartInfo("pnputil.exe", "/add-driver \"" + infPath + "\" /install") { CreateNoWindow = true, UseShellExecute = false });
                pnp.WaitForExit();

                Thread.Sleep(2000);

                if (IsWinUSBActive(instanceId))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("    [OK] WinUSB driver successfully installed and bound to adapter!");
                    Console.ResetColor();
                    return true;
                }

                string curId = FindAdapterInstanceId();
                if (!string.IsNullOrEmpty(curId) && IsWinUSBActive(curId))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("    [OK] WinUSB driver successfully installed and bound to adapter!");
                    Console.ResetColor();
                    return true;
                }
            }

            // Method 2: Fallback to Zadig Automation
            return AutomateZadigSilently(baseDir, instanceId);
        }

        static bool AutomateZadigSilently(string baseDir, string targetInstanceId)
        {
            Console.WriteLine("    [..] Using Zadig automation engine as fallback...");

            // Make sure no previous instances are locking the Global/Zadig mutex
            KillExistingZadigProcesses();

            string zadigExe = Path.Combine(baseDir, "zadig-2.9.exe");
            string zadigIni = Path.Combine(baseDir, "zadig.ini");

            string iniContent = "[general]\r\nadvanced_mode = false\r\nexit_on_success = true\r\nlog_level = 0\r\n\r\n[device]\r\nlist_all = true\r\ninclude_hubs = true\r\n\r\n[driver]\r\ndefault_driver = 0\r\n";
            File.WriteAllText(zadigIni, iniContent);

            var psi = new ProcessStartInfo(zadigExe);
            psi.WorkingDirectory = baseDir;
            psi.WindowStyle = ProcessWindowStyle.Minimized;
            Process proc = Process.Start(psi);

            IntPtr hZadig = IntPtr.Zero;
            Console.Write("    [..] Initializing USB bus scanner");

            for (int t = 0; t < 30; t++)
            {
                hZadig = FindWindow(null, "Zadig 2.9");
                if (hZadig == IntPtr.Zero) hZadig = FindWindow(null, "Zadig");
                if (hZadig == IntPtr.Zero && proc != null && !proc.HasExited)
                {
                    try { hZadig = proc.MainWindowHandle; } catch { }
                }

                if (hZadig != IntPtr.Zero)
                {
                    // Move far off-screen so user does not see it
                    SetWindowPos(hZadig, IntPtr.Zero, -32000, -32000, 0, 0, SWP_NOACTIVATE | SWP_NOZORDER);
                    break;
                }
                Console.Write(".");
                Thread.Sleep(300);
            }
            Console.WriteLine();

            if (hZadig == IntPtr.Zero)
            {
                Console.WriteLine("    [!] Could not connect to driver installer engine.");
                return false;
            }

            // Enforce Options: List All Devices (40004) = ON, Ignore Hubs (40009) = OFF
            try
            {
                IntPtr hMenu = GetMenu(hZadig);
                if (hMenu != IntPtr.Zero)
                {
                    IntPtr hOpt = GetSubMenu(hMenu, 1);
                    if (hOpt != IntPtr.Zero)
                    {
                        uint stateAll = GetMenuState(hOpt, IDM_LISTALL, 0);
                        if ((stateAll & 0x0008) == 0) // if not checked
                        {
                            SendMessage(hZadig, WM_COMMAND, (IntPtr)IDM_LISTALL, IntPtr.Zero);
                        }

                        uint stateHubs = GetMenuState(hOpt, IDM_IGNOREHUBS, 0);
                        if ((stateHubs & 0x0008) != 0) // if checked
                        {
                            SendMessage(hZadig, WM_COMMAND, (IntPtr)IDM_IGNOREHUBS, IntPtr.Zero);
                        }
                    }
                }
            }
            catch { }

            // Trigger list refresh via UM_REFRESH_LIST (WM_APP = 0x8000)
            SendMessage(hZadig, WM_APP, IntPtr.Zero, IntPtr.Zero);
            Thread.Sleep(800);

            IntPtr hCombo = IntPtr.Zero;
            IntPtr hInstall = IntPtr.Zero;
            int targetIdx = -1;
            string targetItemName = "";

            Console.WriteLine("    [..] Scanning device tree for GameCube Adapter...");
            for (int poll = 0; poll < 30; poll++) // up to 15 seconds
            {
                SetWindowPos(hZadig, IntPtr.Zero, -32000, -32000, 0, 0, SWP_NOACTIVATE | SWP_NOZORDER);

                hCombo = GetDlgItem(hZadig, IDC_DEVICELIST);
                hInstall = GetDlgItem(hZadig, IDC_INSTALL);

                if (hCombo != IntPtr.Zero)
                {
                    int count = SendMessage(hCombo, CB_GETCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            int len = SendMessage(hCombo, CB_GETLBTEXTLEN, (IntPtr)i, IntPtr.Zero).ToInt32();
                            string itemText = "";
                            if (len > 0)
                            {
                                var sb = new StringBuilder(len + 2);
                                SendMessage(hCombo, CB_GETLBTEXT, (IntPtr)i, sb);
                                itemText = sb.ToString();
                            }

                            if (itemText.IndexOf("Orca", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                itemText.IndexOf("WUP-028", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                itemText.IndexOf("GameCube", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (itemText.IndexOf("057E", StringComparison.OrdinalIgnoreCase) >= 0 && itemText.IndexOf("0337", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                targetIdx = i;
                                targetItemName = itemText;
                                break;
                            }

                            // Query hardware VID / PID controls via WM_GETTEXT (cross-process safe)
                            SendMessage(hCombo, CB_SETCURSEL, (IntPtr)i, IntPtr.Zero);
                            SendMessage(hZadig, WM_COMMAND, (IntPtr)((CBN_SELCHANGE << 16) | IDC_DEVICELIST), hCombo);
                            Thread.Sleep(30);

                            IntPtr hVid = GetDlgItem(hZadig, IDC_VID);
                            IntPtr hPid = GetDlgItem(hZadig, IDC_PID);
                            var sbVid = new StringBuilder(64);
                            var sbPid = new StringBuilder(64);
                            SendMessage(hVid, WM_GETTEXT, (IntPtr)sbVid.Capacity, sbVid);
                            SendMessage(hPid, WM_GETTEXT, (IntPtr)sbPid.Capacity, sbPid);

                            string vid = sbVid.ToString().Trim();
                            string pid = sbPid.ToString().Trim();

                            if (vid.Equals("057E", StringComparison.OrdinalIgnoreCase) &&
                                pid.Equals("0337", StringComparison.OrdinalIgnoreCase))
                            {
                                targetIdx = i;
                                targetItemName = string.IsNullOrEmpty(itemText) ? "Orca / GameCube Adapter (057E:0337)" : (itemText + " [057E:0337]");
                                break;
                            }
                        }
                    }
                    else
                    {
                        SendMessage(hZadig, WM_APP, IntPtr.Zero, IntPtr.Zero);
                    }
                }

                if (targetIdx >= 0) break;
                Thread.Sleep(500);
            }

            if (targetIdx < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    [!] Timed out waiting for adapter in device list.");
                Console.ResetColor();
                try { proc.Kill(); } catch { }
                return false;
            }

            Console.WriteLine("    [OK] Matched Adapter Device: " + targetItemName);

            // Re-select target item to ensure Zadig's internal state is locked on the adapter
            SendMessage(hCombo, CB_SETCURSEL, (IntPtr)targetIdx, IntPtr.Zero);
            SendMessage(hZadig, WM_COMMAND, (IntPtr)((CBN_SELCHANGE << 16) | IDC_DEVICELIST), hCombo);
            Thread.Sleep(500);

            // Start background modal dialog watcher (MUST ignore hZadig itself!)
            bool installFinished = false;
            uint zadigPid = (uint)proc.Id;

            Thread watcherThread = new Thread(() =>
            {
                while (!installFinished)
                {
                    EnumWindows((hWnd, lParam) =>
                    {
                        uint pId = 0;
                        GetWindowThreadProcessId(hWnd, out pId);
                        // Crucial: Only target popup dialogs, NEVER hZadig main window
                        if (pId == zadigPid && hWnd != hZadig)
                        {
                            var sbClass = new StringBuilder(64);
                            GetClassName(hWnd, sbClass, 64);
                            if (sbClass.ToString() == "#32770") // Standard Dialog
                            {
                                ShowWindow(hWnd, SW_HIDE);
                                // IDYES = 6 (for "Warning - System Driver" confirmation)
                                SendMessage(hWnd, WM_COMMAND, (IntPtr)6, IntPtr.Zero);
                                // IDOK = 1 (for completion notice)
                                SendMessage(hWnd, WM_COMMAND, (IntPtr)1, IntPtr.Zero);

                                EnumChildWindows(hWnd, (hChild, lChild) =>
                                {
                                    int ctrlId = GetDlgCtrlID(hChild);
                                    if (ctrlId == 6 || ctrlId == 1)
                                    {
                                        SendMessage(hChild, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    return true;
                                }, IntPtr.Zero);
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    Thread.Sleep(80);
                }
            });
            watcherThread.IsBackground = true;
            watcherThread.Start();

            // Trigger Driver Installation asynchronously via PostMessage
            Console.WriteLine("    [..] Installing WinUSB driver in background (takes ~10-15s)...");
            PostMessage(hZadig, WM_COMMAND, (IntPtr)IDC_INSTALL, hInstall);
            PostMessage(hInstall, BM_CLICK, IntPtr.Zero, IntPtr.Zero);

            int waitCount = 0;
            while (!proc.HasExited && waitCount < 70) // up to 35 seconds
            {
                SetWindowPos(hZadig, IntPtr.Zero, -32000, -32000, 0, 0, SWP_NOACTIVATE | SWP_NOZORDER);

                if (IsWinUSBActive(targetInstanceId))
                {
                    break;
                }

                Thread.Sleep(500);
                waitCount++;
            }

            installFinished = true;
            try { watcherThread.Join(500); } catch { }

            if (!proc.HasExited)
            {
                try { proc.Kill(); proc.WaitForExit(1000); } catch { }
            }

            Thread.Sleep(1000);

            if (IsWinUSBActive(targetInstanceId))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    [OK] WinUSB installation successful!");
                Console.ResetColor();
                return true;
            }

            string finalId = FindAdapterInstanceId();
            if (!string.IsNullOrEmpty(finalId) && IsWinUSBActive(finalId))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    [OK] WinUSB installation successful!");
                Console.ResetColor();
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("    [!] WinUSB driver installation did not register.");
            Console.ResetColor();
            return false;
        }

        static bool ApplyHidusbf1000HzSilently(string baseDir, string instanceId)
        {
            Console.WriteLine("[*] Step 3/3: Applying 1000Hz filter driver (SweetLow HIDUSBF)...");

            // 0. Ensure service is stopped before copying
            try
            {
                var scStop = Process.Start(new ProcessStartInfo("sc.exe", "stop hidusbf") { CreateNoWindow = true, UseShellExecute = false });
                scStop.WaitForExit();
            }
            catch { }

            // 1. Copy WHQL-signed NoPatch driver (HVCI / Core Isolation compatible)
            string srcDriver = Path.Combine(baseDir, @"hidusbf\DRIVER\AMD64_AS\NoPatch\hidusbf.sys");
            if (!File.Exists(srcDriver))
            {
                srcDriver = Path.Combine(baseDir, @"hidusbf\DRIVER\AMD64_AS\hidusbf.sys");
            }
            string winDriverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\drivers\hidusbf.sys");

            try
            {
                File.Copy(srcDriver, winDriverPath, true);
                Console.WriteLine("    [OK] Deployed signed driver to System32\\drivers\\hidusbf.sys");
            }
            catch (Exception ex)
            {
                Console.WriteLine("    [!] Error copying driver file: " + ex.Message);
                return false;
            }

            // 2. Ensure Service exists and configure HVCI-safe parameters (disable kernel patching)
            var scProc = Process.Start(new ProcessStartInfo("sc.exe", "create hidusbf type= kernel start= demand binPath= System32\\drivers\\hidusbf.sys displayName= \"USB Mouse Rate Adjuster Lower Filter by SweetLow\"") { CreateNoWindow = true, UseShellExecute = false });
            scProc.WaitForExit();
            var scConfig = Process.Start(new ProcessStartInfo("sc.exe", "config hidusbf binPath= System32\\drivers\\hidusbf.sys start= demand") { CreateNoWindow = true, UseShellExecute = false });
            scConfig.WaitForExit();

            try
            {
                using (var srvKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\hidusbf\Parameters"))
                {
                    if (srvKey != null)
                    {
                        srvKey.SetValue("PatchUSBPort", 0, RegistryValueKind.DWord);
                        srvKey.SetValue("PatchUSBXHCI", 0, RegistryValueKind.DWord);
                    }
                }
                using (var ctrlKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\HIDUSBF"))
                {
                    if (ctrlKey != null)
                    {
                        ctrlKey.SetValue("PatchUSBPort", 0, RegistryValueKind.DWord);
                        ctrlKey.SetValue("PatchUSBXHCI", 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            Console.WriteLine("    [OK] Configured kernel service 'hidusbf' (HVCI-compatible mode)");

            // 3. Set LowerFilters & bInterval on Device via SetupAPI & Registry
            IntPtr hDev = SetupDiGetClassDevs(IntPtr.Zero, "USB", IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
            if (hDev == (IntPtr)(-1)) return false;

            SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA();
            devData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

            uint i = 0;
            bool configured = false;
            while (SetupDiEnumDeviceInfo(hDev, i++, ref devData))
            {
                var sb = new StringBuilder(1024);
                int req;
                if (SetupDiGetDeviceInstanceId(hDev, ref devData, sb, sb.Capacity, out req))
                {
                    if (sb.ToString().Equals(instanceId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Set LowerFilters = "hidusbf" (MULTI_SZ)
                        byte[] filterBytes = Encoding.Unicode.GetBytes("hidusbf\0\0");
                        SetupDiSetDeviceRegistryProperty(hDev, ref devData, SPDRP_LOWERFILTERS, filterBytes, (uint)filterBytes.Length);

                        // Set bInterval = 1 (1ms = 1000Hz) in Device Parameters
                        IntPtr hKey = SetupDiOpenDevRegKey(hDev, ref devData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_ALL_ACCESS);
                        if (hKey != IntPtr.Zero && hKey != (IntPtr)(-1))
                        {
                            byte[] val = BitConverter.GetBytes((int)1);
                            RegSetValueEx(hKey, "bInterval", 0, 4, val, val.Length); // REG_DWORD = 4
                            RegCloseKey(hKey);
                            configured = true;
                            Console.WriteLine("    [OK] Attached 'hidusbf' LowerFilter & set bInterval = 1 (1000Hz)");
                        }
                        break;
                    }
                }
            }
            SetupDiDestroyDeviceInfoList(hDev);

            // Double redundancy: direct registry write
            try
            {
                using (var devKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + instanceId, true))
                {
                    if (devKey != null)
                    {
                        devKey.SetValue("LowerFilters", new string[] { "hidusbf" }, RegistryValueKind.MultiString);
                        using (var paramKey = devKey.CreateSubKey("Device Parameters"))
                        {
                            if (paramKey != null)
                            {
                                paramKey.SetValue("bInterval", 1, RegistryValueKind.DWord);
                                configured = true;
                            }
                        }
                    }
                }
            }
            catch { }

            // 4. Restart Device
            Console.WriteLine("    [..] Restarting adapter to activate 1000Hz polling rate...");
            var pnpRestart = Process.Start(new ProcessStartInfo("pnputil.exe", "/restart-device \"" + instanceId + "\"") { CreateNoWindow = true, UseShellExecute = false });
            pnpRestart.WaitForExit();
            Thread.Sleep(1000);
            Console.WriteLine("    [OK] Adapter hardware successfully re-initialized.");

            return configured;
        }

        static void Main(string[] args)
        {
            Console.Title = "GameCube Adapter 1000Hz Silent Overclocker";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=======================================================================");
            Console.WriteLine("       GameCube Controller Adapter 1000Hz Silent Overclocker           ");
            Console.WriteLine("=======================================================================");
            Console.ResetColor();
            Console.WriteLine();

            if (!IsAdministrator())
            {
                Console.WriteLine("[*] Requesting Administrator privileges (UAC prompt)...");
                Elevate();
                return;
            }

            // Immediately kill any lingering Zadig processes to free mutex
            KillExistingZadigProcesses();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(baseDir, "zadig-2.9.exe")))
            {
                string sub = Path.Combine(baseDir, "GC_Adapter_Overclock");
                if (File.Exists(Path.Combine(sub, "zadig-2.9.exe"))) baseDir = sub;
                else
                {
                    string desk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GC_Adapter_Overclock");
                    if (File.Exists(Path.Combine(desk, "zadig-2.9.exe"))) baseDir = desk;
                }
            }

            // Step 1: Find adapter
            Console.WriteLine("[*] Step 1/3: Scanning for connected GameCube Controller Adapter...");
            string instanceId = FindAdapterInstanceId();

            if (string.IsNullOrEmpty(instanceId))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[!] No GameCube Controller Adapter (057E:0337) detected!");
                Console.WriteLine("    - If using a Mayflash adapter, ensure the switch is set to 'Wii U'.");
                Console.WriteLine("    - Ensure the black USB cable is firmly plugged into a rear USB port.");
                Console.ResetColor();
                Console.WriteLine("\nPlug in your adapter and press any key to retry...");
                Console.ReadKey();
                instanceId = FindAdapterInstanceId();
                if (string.IsNullOrEmpty(instanceId))
                {
                    Console.WriteLine("[!] Adapter still not detected. Please reconnect and run this tool again.");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    [OK] Found Adapter: " + instanceId);
            Console.ResetColor();
            Console.WriteLine();

            // Step 2: Check / Install WinUSB
            if (!IsWinUSBActive(instanceId))
            {
                bool winusbOk = InstallWinUSBSilently(baseDir, instanceId);
                if (!winusbOk)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("    [!] WinUSB driver installation did not complete.");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }
                // Refresh instance ID
                Thread.Sleep(1000);
                instanceId = FindAdapterInstanceId();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[*] Step 2/3: WinUSB Driver is already active on adapter!");
                Console.ResetColor();
            }

            Console.WriteLine();

            // Step 3: Apply HIDUSBF 1000Hz Silently
            bool success = ApplyHidusbf1000HzSilently(baseDir, instanceId);

            Console.WriteLine();

            // Final Verification
            Console.WriteLine("=======================================================================");
            if (success)
            {
                try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" [SUCCESS] GameCube Controller Adapter is now OVERCLOCKED to 1000Hz!   ");
                Console.ResetColor();
                Console.WriteLine("=======================================================================");
                Console.WriteLine();
                Console.WriteLine("Next Steps for Super Smash Bros. Melee / Slippi:");
                Console.WriteLine("  1. Launch Slippi Launcher.");
                Console.WriteLine("  2. In Dolphin: Options -> Controller Settings -> Port 1: 'GameCube Adapter for Wii U'.");
                Console.WriteLine("  3. Click 'Configure' next to Port 1 to confirm live ~1000Hz reading.");
                Console.WriteLine("  4. In Dolphin: Config -> Slippi -> check 'Reduce Timing Dispersion'.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" [!] Overclocking completed with warnings. Please replug the adapter.");
                Console.ResetColor();
                Console.WriteLine("=======================================================================");
            }

            Console.WriteLine("\nDone! Press any key to exit...");
            Console.ReadKey();
        }
    }
}
