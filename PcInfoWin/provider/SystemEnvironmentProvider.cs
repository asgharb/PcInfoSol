using SqlDataExtention.Entity;
using System;
using PcInfoWin.Provider;

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
                OsVersion = Environment.OSVersion.VersionString,
                IsRealVNCInstalled= FindProgram.IsProgramInstalled()
            };
            return info;
        }
    }
}
