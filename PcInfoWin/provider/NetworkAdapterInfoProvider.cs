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
                       "SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = true AND MACAddress IS NOT NULL"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "Unknown";
                    string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
                    string adapterType = obj["AdapterType"]?.ToString()?.ToLower() ?? "";
                    bool isEnabled = (bool)(obj["NetEnabled"] ?? false);

                    // حذف کارت‌های مجازی، Bluetooth، Tunnel، WAN، Virtual و Loopback
                    string lowerName = name.ToLower();
                    if (lowerName.Contains("virtual") ||
                        lowerName.Contains("vpn") ||
                        lowerName.Contains("bluetooth") ||
                        lowerName.Contains("loopback") ||
                        lowerName.Contains("tunnel") ||
                        lowerName.Contains("wan"))
                    {
                        continue;
                    }

                    // تشخیص نوع LAN یا Wi-Fi
                    bool isLAN = false;
                    if (adapterType.Contains("ethernet") || lowerName.Contains("ethernet") || lowerName.Contains("lan"))
                        isLAN = true;
                    else if (adapterType.Contains("wireless") || lowerName.Contains("wi-fi") || lowerName.Contains("wifi"))
                        isLAN = false;
                    else
                        continue; // اگر نه LAN بود نه Wi-Fi، بی‌خیال

                    // گرفتن IP آدرس
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

                    adapters.Add(new NetworkAdapterInfo
                    {
                        Name = name,
                        MACAddress = mac,
                        IpAddress = ip,
                        //IsPhysical = true,
                        IsEnabled = isEnabled,
                        IsLAN = isLAN
                    });
                }
            }

            // کارت‌های فعال بیایند اول
            return adapters
                .OrderByDescending(a => a.IsEnabled)
                .ToList();
        }
    }
}
