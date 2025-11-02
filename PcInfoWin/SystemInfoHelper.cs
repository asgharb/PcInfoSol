using PcInfoWin.Properties;
using PcInfoWin.Provider;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PcInfoWin
{
    public static class SystemInfoHelper
    {
        public static SystemInfo GetCurentSystemInfo()
        {
            try
            {
                SystemInfo systemInfo = new SystemInfo();

                //systemInfo.InsertDate = DateTime.Now;
                //systemInfo.ExpireDate = null;

                // ---------- System Environment ----------
                var sysEnvProvider = new SystemEnvironmentProvider();
                systemInfo.systemEnvironmentInfo = sysEnvProvider.GetSystemEnvironmentInfo();

                // ---------- CPU ----------
                var cpuProvider = new CpuInfoProvider();
                systemInfo.cpuInfo = cpuProvider.GetCpuInfo();

                // ---------- GPU ----------
                var gpuProvider = new GpuInfoProvider();
                systemInfo.gpuInfo = gpuProvider.GetGpuInfo();

                // ---------- Motherboard ----------
                var mbProvider = new MotherboardInfoProvider();
                systemInfo.motherboardInfo = mbProvider.GetMotherboardInfo();

                // ---------- RAM ----------
                var ramProvider = new RamInfoProvider();
                systemInfo.RamSummaryInfo = ramProvider.GetRamSummary();
                systemInfo.RamModuleInfo = ramProvider.GetRamModules();

                // ---------- Disk ----------
                var diskProvider = new DiskInfoProvider();
                systemInfo.DiskInfo = diskProvider.GetDisks();

                // ---------- Network Adapter ----------
                var netProvider = new NetworkAdapterInfoProvider();
                systemInfo.NetworkAdapterInfo = netProvider.GetAllNetworkAdapters();

                // ---------- Optical Drive ----------
                var opticalProvider = new OpticalDriveInfoProvider();
                systemInfo.OpticalDriveInfo = opticalProvider.GetAllOpticalDrives();

                // ---------- Monitor ----------
                var monitorProvider = new MonitorInfoProvider();
                systemInfo.monitorInfo = monitorProvider.GetAllMonitors();

                // ---------- UpdateInfo ----------
                var updateInfo = new UpdateInfo(
                    (!string.IsNullOrEmpty(Settings.Default.PathUpdate) && Settings.Default.PathUpdate.Length >= 5)
                        ? Settings.Default.PathUpdate
                        : Program.defaultUpdatePath
                );

                systemInfo.updateInfo = updateInfo;


                PcCodeInfo pcCodeInfo = new PcCodeInfo();
                systemInfo.pcCodeInfo = new List<SqlDataExtention.Entity.PcCodeInfo>();
                systemInfo.pcCodeInfo.Add(pcCodeInfo);

                return systemInfo;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "---", SysId: Settings.Default.SystemInfoID > 0 ? Settings.Default.SystemInfoID : 0); return null;
            }
        }


    }
}
