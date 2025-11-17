using Microsoft.Win32;
using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PcInfoWin.Provider
{
    public class SystemEnvironmentProvider
    {
        public List<SystemEnvironmentInfo> GetSystemEnvironmentInfo()
        {
            List<SystemEnvironmentInfo> systemEnvironmentInfo = new List<SystemEnvironmentInfo>();
            SystemEnvironmentInfo info = new SystemEnvironmentInfo
            {
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                Domain = Environment.UserDomainName,
                OperatingSystem = GetWindowsVersion(),
                //OsVersion = Environment.OSVersion.VersionString,
                IsRealVNCInstalled = FindProgram.IsVncInstalled(),
                IsSemanticInstalled = FindProgram.IsSemanticInstalled(),
                AppVersion = FileVersionInfo
               .GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
               .ProductVersion
            };
            systemEnvironmentInfo.Add(info);
            return systemEnvironmentInfo;
        }

        public  string GetWindowsVersion()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string productName = key.GetValue("ProductName")?.ToString() ?? "";
                        string displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                        string releaseId = key.GetValue("ReleaseId")?.ToString() ?? "";

                        // اگر ProductName شامل "11" باشد، یعنی Windows 11 است
                        if (productName.Contains("11"))
                            return $"Windows 11 ({displayVersion})";
                        else if (productName.Contains("10"))
                            return $"Windows 10 ({displayVersion})";

                        // در غیر این صورت فقط همان نام را برگردان
                        return productName;
                    }
                }
            }
            catch 
            {

            }
            return "Unknown Windows Version";
        }
    }
}
