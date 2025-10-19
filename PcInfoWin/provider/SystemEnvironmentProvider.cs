using SqlDataExtention.Entity;
using System;
using System.Runtime.InteropServices;

namespace PcInfoWin.Provider
{
    public class SystemEnvironmentProvider
    {
        public SystemEnvironmentInfo GetSystemEnvironmentInfo()
        {
            var info = new SystemEnvironmentInfo
            {
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                Domain = Environment.UserDomainName,
                OperatingSystem = RuntimeInformation.OSDescription,
                OsVersion = Environment.OSVersion.VersionString,
            };
            return info;
        }
    }
}
