using SqlDataExtention.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace PcInfoWin.Provider
{
    public class NetworkAdapterInfoProvider
    {
        public List<NetworkAdapterInfo> GetAllNetworkAdapters()
        {
            var adapters = new List<NetworkAdapterInfo>();

            using (var searcher = new ManagementObjectSearcher(
                       "SELECT * FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    bool isPhysical = (bool)(obj["PhysicalAdapter"] ?? false);
                    bool isEnabled = (bool)(obj["NetEnabled"] ?? false);
                    string name = obj["Name"]?.ToString() ?? "Unknown";
                    string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
                    string adapterType = obj["AdapterType"]?.ToString() ?? "";


                    // فیلتر کارت‌های مجازی
                    if (!isPhysical) continue;

                    bool isLAN = adapterType.ToLower().Contains("ethernet");

                    adapters.Add(new NetworkAdapterInfo
                    {
                        Name = name,
                        MACAddress = mac,
                        IsPhysical = isPhysical,
                        IsEnabled = isEnabled,
                        IsLAN = isLAN,
                    });
                }
            }

            // اولویت: کارت LAN قبل از دیگر کارت‌ها
            return adapters.OrderByDescending(a => a.IsLAN).ToList();
        }
    }
}
