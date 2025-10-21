using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace PcInfoWin.Provider
{
    public class RamInfoProvider
    {
        public List<RamModuleInfo> GetRamModules()
        {
            var modules = new List<RamModuleInfo>();

            try
            {

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        int capacityGB = (int)(Convert.ToUInt64(mo["Capacity"]) / (1024 * 1024 * 1024));

                        ushort memType = 0;
                        if (mo["SMBIOSMemoryType"] != null)
                            memType = Convert.ToUInt16(mo["SMBIOSMemoryType"]);


                        string memoryType;
                        switch (memType)
                        {
                            case 20: memoryType = "DDR"; break;
                            case 21: memoryType = "DDR2"; break;
                            case 24: memoryType = "DDR3"; break;
                            case 26: memoryType = "DDR4"; break;
                            default: memoryType = "Unknown"; break;
                        }

                        modules.Add(new RamModuleInfo
                        {
                            Manufacturer = mo["Manufacturer"]?.ToString() ?? "Unknown",
                            CapacityGB = capacityGB,
                            SpeedMHz = Convert.ToInt32(mo["Speed"] ?? 0),
                            MemoryType = memoryType,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error reading RAM info: " + ex.Message);
            }

            return modules;
        }
        public RamSummaryInfo GetRamSummary()
        {
            var modules = GetRamModules();

            return new RamSummaryInfo
            {
                TotalModules = modules.Count,
                TotalCapacityGB = modules.Sum(m => (int)m.CapacityGB)
            };
        }
    }
}
