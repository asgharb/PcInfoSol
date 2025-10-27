using System;
using System.IO;

namespace PcInfoWin.Provider
{
    public static class FindProgram
    {
        public  static bool IsProgramInstalled()
        {
            //string programName = "vncs";
            //string programName1 = "vncguihelper";
            //string programName2 = "RealVNC";

            //List<string> programNames = new List<string>();
            //string[] registryKeys = new string[]
            //{
            //@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            //@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            //};

            //foreach (string key in registryKeys)
            //{
            //    using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(key))
            //    {
            //        if (rk == null) continue;

            //        foreach (string subKeyName in rk.GetSubKeyNames())
            //        {
            //            using (RegistryKey sk = rk.OpenSubKey(subKeyName))
            //            {
            //                var name = sk.GetValue("DisplayName") as string;
            //                if(name == null) continue;
            //                programNames.Add(name);
            //                if (!string.IsNullOrEmpty(name) && (name.Contains(programName) || name.Contains(programName1) || name.Contains(programName2)))
            //                {
            //                    return true;
            //                }
            //            }
            //        }
            //    }
            //}

            string programPath = @"C:\Program Files\RealVNC\VNC Server\VNCServer.exe"; // مسیر اصلی exe
            if (File.Exists(programPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
