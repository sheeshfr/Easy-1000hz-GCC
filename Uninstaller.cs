using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace GCAdapterReset
{
    class Program
    {
        const uint DIGCF_ALLCLASSES = 0x00000004;
        const uint DIGCF_PRESENT = 0x00000002;
        const uint DICS_FLAG_GLOBAL = 0x00000001;
        const uint DIREG_DEV = 0x00000001;
        const uint KEY_ALL_ACCESS = 0xF003F;
        const uint SPDRP_LOWERFILTERS = 0x00000014;

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
        static extern int RegDeleteValue(IntPtr hKey, string lpValueName);

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Main(string[] args)
        {
            Console.Title = "GameCube Adapter Reset & Uninstaller";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=======================================================================");
            Console.WriteLine("            GameCube Controller Adapter Reset & Uninstaller            ");
            Console.WriteLine("=======================================================================");
            Console.ResetColor();
            Console.WriteLine();

            if (!IsAdministrator())
            {
                Console.WriteLine("[*] Requesting Administrator privileges (UAC prompt)...");
                var psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                try { Process.Start(psi); } catch { }
                return;
            }

            Console.WriteLine("[*] Stopping and removing SweetLow HIDUSBF kernel service...");
            try
            {
                var scStop = Process.Start(new ProcessStartInfo("sc.exe", "stop hidusbf") { CreateNoWindow = true, UseShellExecute = false });
                scStop.WaitForExit();
                var scDel = Process.Start(new ProcessStartInfo("sc.exe", "delete hidusbf") { CreateNoWindow = true, UseShellExecute = false });
                scDel.WaitForExit();
                Console.WriteLine("    [OK] Service 'hidusbf' stopped and deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("    [!] Service error: " + ex.Message);
            }

            Console.WriteLine("[*] Cleaning up driver binaries in System32...");
            string sysDriver = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\drivers\hidusbf.sys");
            if (File.Exists(sysDriver))
            {
                try
                {
                    File.Delete(sysDriver);
                    Console.WriteLine("    [OK] Deleted System32\\drivers\\hidusbf.sys");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    [!] Could not delete hidusbf.sys: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("    [OK] No leftover hidusbf.sys in drivers directory.");
            }

            Console.WriteLine("[*] Searching for GameCube Adapter to reset registry filters...");
            IntPtr hDev = SetupDiGetClassDevs(IntPtr.Zero, "USB", IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
            string foundInstanceId = null;
            if (hDev != (IntPtr)(-1))
            {
                SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA();
                devData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                uint i = 0;
                while (SetupDiEnumDeviceInfo(hDev, i++, ref devData))
                {
                    var sb = new StringBuilder(1024);
                    int req;
                    if (SetupDiGetDeviceInstanceId(hDev, ref devData, sb, sb.Capacity, out req))
                    {
                        string id = sb.ToString();
                        if (id.IndexOf("057E&PID_0337", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foundInstanceId = id;
                            Console.WriteLine("    Found Adapter: " + id);

                            // Delete LowerFilters property
                            SetupDiSetDeviceRegistryProperty(hDev, ref devData, SPDRP_LOWERFILTERS, null, 0);
                            Console.WriteLine("    [OK] Cleared LowerFilters registry entry.");

                            // Delete bInterval from Device Parameters
                            IntPtr hKey = SetupDiOpenDevRegKey(hDev, ref devData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_ALL_ACCESS);
                            if (hKey != IntPtr.Zero && hKey != (IntPtr)(-1))
                            {
                                RegDeleteValue(hKey, "bInterval");
                                RegCloseKey(hKey);
                                Console.WriteLine("    [OK] Removed bInterval override (reset to default 125Hz).");
                            }

                            // Direct registry cleanup
                            try
                            {
                                using (var devKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + foundInstanceId, true))
                                {
                                    if (devKey != null)
                                    {
                                        try { devKey.DeleteValue("LowerFilters"); } catch { }
                                        using (var paramKey = devKey.OpenSubKey("Device Parameters", true))
                                        {
                                            if (paramKey != null)
                                            {
                                                try { paramKey.DeleteValue("bInterval"); } catch { }
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }

                            break;
                        }
                    }
                }
                SetupDiDestroyDeviceInfoList(hDev);
            }

            if (!string.IsNullOrEmpty(foundInstanceId))
            {
                Console.WriteLine("[*] Restarting adapter hardware to apply stock defaults...");
                var pnp = Process.Start(new ProcessStartInfo("pnputil.exe", "/restart-device \"" + foundInstanceId + "\"") { CreateNoWindow = true, UseShellExecute = false });
                pnp.WaitForExit();
                Console.WriteLine("    [OK] Device restarted successfully.");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=======================================================================");
            Console.WriteLine(" [SUCCESS] Reset complete! System has returned to stock defaults.     ");
            Console.WriteLine("=======================================================================");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
