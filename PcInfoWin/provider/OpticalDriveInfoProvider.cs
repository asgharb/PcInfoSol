using SqlDataExtention.Entity;
using System.Collections.Generic;
using System.Management;


namespace PcInfoWin.Provider
{
    public class OpticalDriveInfoProvider
    {
        public List<OpticalDriveInfo> GetAllOpticalDrives()
        {
            var drives = new List<OpticalDriveInfo>();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_CDROMDrive"))
            {
                foreach (ManagementObject drive in searcher.Get())
                {
                    drives.Add(new OpticalDriveInfo
                    {
                        Name = drive["Name"]?.ToString() ?? "Unknown",
                        Manufacturer = drive["Manufacturer"]?.ToString() ?? "Unknown",
                        MediaType = drive["MediaType"]?.ToString() ?? "Unknown"
                    });
                }
            }

            return drives;
        }
    }
}
