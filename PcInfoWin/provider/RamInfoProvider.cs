using Hardware.Info;
using PcInfoWin.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Provider
{
    //public class RamInfoProvider
    //{
    //    private readonly HardwareInfo _hardwareInfo;

    //    public RamInfoProvider()
    //    {
    //        _hardwareInfo = new HardwareInfo();
    //        _hardwareInfo.RefreshMemoryStatus();
    //    }

    //    public List<RamModuleInfo> GetRamModules(int systemInfoRef = 0)
    //    {
    //        var modules = new List<RamModuleInfo>();

    //        _hardwareInfo.RefreshMemoryList(); // ← مهم

    //        foreach (var mem in _hardwareInfo.MemoryList)
    //        {
    //            modules.Add(new RamModuleInfo
    //            {
    //                Manufacturer = mem.Manufacturer ?? "Unknown",
    //                CapacityGB = mem.Capacity / (1024 * 1024 * 1024),
    //                SpeedMHz = mem.Speed,
    //                MemoryType = "Unknown",
    //                SystemInfoRef = systemInfoRef
    //            });
    //        }

    //        return modules;
    //    }

    //    public RamSummaryInfo GetRamSummary(int systemInfoRef = 0)
    //    {
    //        var modules = GetRamModules(systemInfoRef);

    //        return new RamSummaryInfo
    //        {
    //            TotalModules = modules.Count,
    //            TotalCapacityGB = modules.Aggregate<RamModuleInfo, ulong>(0, (sum, m) => sum + m.CapacityGB),
    //            SystemInfoRef = systemInfoRef
    //        };
    //    }
    //}


    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;

    namespace PcInfoWin.Provider
    {
        public class RamInfoProvider
        {

            public List<RamModuleInfo> GetRamModules(int systemInfoRef = 0)
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
                            if (mo["MemoryType"] != null)
                                memType = Convert.ToUInt16(mo["MemoryType"]);

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
                                CapacityGB = capacityGB, // توجه: در مدل شما CapacityMB است، حالا GB ست کردیم
                                SpeedMHz = Convert.ToInt32(mo["Speed"] ?? 0),
                                MemoryType = memoryType,
                                SystemInfoRef = systemInfoRef
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading RAM info: " + ex.Message);
                }

                return modules;
            }
            public RamSummaryInfo GetRamSummary(int systemInfoRef = 0)
            {
                var modules = GetRamModules(systemInfoRef);

                return new RamSummaryInfo
                {
                    TotalModules = modules.Count,
                    TotalCapacityGB = modules.Sum(m => (int)m.CapacityGB),
                    SystemInfoRef = systemInfoRef
                };
            }
        }
    }

}
