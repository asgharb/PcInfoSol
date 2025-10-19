﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlDataExtention.Entity.Main;
using PcInfoWin.Provider;
using SqlDataExtention.Entity;

namespace PcInfoWin
{
    public static class SystemInfoHelper
    {
        public static SystemInfo GetCurentSystemInfo()
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


            PcCodeInfo pcCodeInfo = new PcCodeInfo();
            systemInfo.pcCodeInfo = new List<SqlDataExtention.Entity.PcCodeInfo>();
            systemInfo.pcCodeInfo.Add(pcCodeInfo);

            return systemInfo;
        }


    }
}
