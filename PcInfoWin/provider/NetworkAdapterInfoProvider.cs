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
            NetworkAdapterInfo motherboardLan = null;

            using (var searcher = new ManagementObjectSearcher(
                       "SELECT * FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    bool isPhysical = (bool)(obj["PhysicalAdapter"] ?? false);
                    string name = obj["Name"]?.ToString() ?? "Unknown";
                    string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
                    string adapterType = obj["AdapterType"]?.ToString() ?? "";

                    if (!isPhysical) continue;

                    bool isLAN = adapterType.ToLower().Contains("ethernet");
                    bool isEnabled = (bool)(obj["NetEnabled"] ?? false);

                    // دریافت IP
                    string ip = "";
                    using (var configSearcher = new ManagementObjectSearcher(
                               $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE MACAddress = '{mac}' AND IPEnabled = true"))
                    {
                        foreach (ManagementObject config in configSearcher.Get())
                        {
                            var addresses = (string[])config["IPAddress"];
                            if (addresses != null && addresses.Length > 0)
                            {
                                ip = addresses[0];
                                break;
                            }
                        }
                    }

                    var adapter = new NetworkAdapterInfo
                    {
                        Name = name,
                        MACAddress = mac,
                        IpAddress = ip,
                        IsPhysical = isPhysical,
                        IsEnabled = isEnabled,
                        IsLAN = isLAN,
                        IsMotherboardLan = false
                    };

                    if (isLAN && motherboardLan == null)
                    {
                        // اولین LAN فیزیکی را کارت مادربرد در نظر می‌گیریم
                        adapter.IsMotherboardLan = true;
                        motherboardLan = adapter;
                    }

                    adapters.Add(adapter);
                }
            }

            // مرتب‌سازی نهایی
            return adapters
                .OrderByDescending(a => a.IsMotherboardLan)              // مادربرد اول
                .ThenByDescending(a => a.IsLAN && a.IsEnabled)          // LAN فعال بعد
                .ThenByDescending(a => a.IsEnabled)                     // فعال بعد
                .ToList();
        }

    }

    //public class NetworkAdapterInfoProvider
    //{
    //    public List<NetworkAdapterInfo> GetAllNetworkAdapters()
    //    {
    //        var adapters = new List<NetworkAdapterInfo>();

    //        using (var searcher = new ManagementObjectSearcher(
    //                   "SELECT * FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL"))
    //        {
    //            foreach (ManagementObject obj in searcher.Get())
    //            {
    //                bool isPhysical = (bool)(obj["PhysicalAdapter"] ?? false);
    //                bool isEnabled = (bool)(obj["NetEnabled"] ?? false);
    //                string name = obj["Name"]?.ToString() ?? "Unknown";
    //                string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
    //                string adapterType = obj["AdapterType"]?.ToString() ?? "";

    //                // فیلتر کارت‌های مجازی
    //                if (!isPhysical) continue;

    //                bool isLAN = adapterType.ToLower().Contains("ethernet");

    //                adapters.Add(new NetworkAdapterInfo
    //                {
    //                    Name = name,
    //                    MACAddress = mac,
    //                    IpAddress = "", // آدرس IP در اینجا پر نمی‌شود
    //                    IsPhysical = isPhysical,
    //                    IsEnabled = isEnabled,
    //                    IsLAN = isLAN,
    //                });
    //            }
    //        }

    //        // اولویت: کارت LAN قبل از دیگر کارت‌ها
    //        return adapters.OrderByDescending(a => a.IsLAN).ToList();
    //    }
    //}
}
