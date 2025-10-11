using PcInfoWin.Attributes;
using PcInfoWin.Data;
using PcInfoWin.Entity.Models;
using PcInfoWin.Provider;
using PcInfoWin.Provider.PcInfoWin.Provider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PcInfoWin.Entity.Main
{
    public class SystemInfo
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("SystemInfoID")]
        public int SystemInfoID { get; set; }
        [Ignore]
        public SystemEnvironmentInfo systemEnvironmentInfo { get; set; }
        [Ignore]
        public CpuInfo cpuInfo { get; set; }
        [Ignore]
        public GpuInfo gppuInfo { get; set; }
        [Ignore]
        public MotherboardInfo motherboardInfo { get; set; }
        [Ignore]
        public RamSummaryInfo RamSummaryInfo { get; set; }
        [Ignore]
        public List<DiskInfo> DiskInfo { get; set; }
        [Ignore]
        public List<NetworkAdapterInfo> NetworkAdapterInfo { get; set; }
        [Ignore]
        public List<RamModuleInfo> RamModuleInfo { get; set; }
        [Ignore]
        public List<OpticalDriveInfo> OpticalDriveInfo { get; set; }
        [Ignore]
        public List<MonitorInfo> monitorInfos { get; set; }

        public bool IsActive { get; set; }

        public DateTime InsertDate { get; set; } = DateTime.Now;

        public DateTime? ExpireDate { get; set; } = null;

        public SystemInfo()
        {
            // ---------- System Environment ----------
            var sysEnvProvider = new SystemEnvironmentProvider();
            systemEnvironmentInfo = sysEnvProvider.GetSystemEnvironmentInfo();

            // ---------- CPU ----------
            var cpuProvider = new CpuInfoProvider();
            cpuInfo = cpuProvider.GetCpuInfo();

            // ---------- GPU ----------
            var gpuProvider = new GpuInfoProvider();
            gppuInfo = gpuProvider.GetGpuInfo();

            // ---------- Motherboard ----------
            var mbProvider = new MotherboardInfoProvider();
            motherboardInfo = mbProvider.GetMotherboardInfo();

            // ---------- RAM ----------
            var ramProvider = new RamInfoProvider();
            RamSummaryInfo = ramProvider.GetRamSummary(SystemInfoID);
            RamModuleInfo = ramProvider.GetRamModules(SystemInfoID);

            // ---------- Disk ----------
            var diskProvider = new DiskInfoProvider();
            DiskInfo = diskProvider.GetDisks();

            // ---------- Network Adapter ----------
            var netProvider = new NetworkAdapterInfoProvider();
            NetworkAdapterInfo = netProvider.GetAllNetworkAdapters();

            // ---------- Optical Drive ----------
            var opticalProvider = new OpticalDriveInfoProvider();
            OpticalDriveInfo = opticalProvider.GetAllOpticalDrives();

            // ---------- Monitor ----------
            var monitorProvider = new MonitorInfoProvider();
            monitorInfos = monitorProvider.GetAllMonitors();
        }

        public void AttachChildEntities()
        {
            var childProps = GetType().GetProperties()
                .Where(p => typeof(BaseEntity).IsAssignableFrom(p.PropertyType));

            foreach (var prop in childProps)
            {
                var value = prop.GetValue(this);
                if (value is BaseEntity entity)
                    entity.SystemInfoRef = this.SystemInfoID;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            // پراپرتی‌های معمولی SystemInfo
            sb.AppendLine($"SystemInfoID: {SystemInfoID}");
            sb.AppendLine($"InsertDate: {InsertDate}");
            sb.AppendLine($"ExpireDate: {ExpireDate}");

            // پراپرتی‌هایی که مدل یا لیست مدل هستند
            void AppendModel(string name, object value)
            {
                if (value == null)
                {
                    sb.AppendLine($"{name}: null");
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    sb.AppendLine($"{name}:");
                    foreach (var item in enumerable)
                    {
                        sb.AppendLine("  - " + (item?.ToString() ?? "null"));
                    }
                }
                else
                {
                    sb.AppendLine($"{name}: {value}");
                }
            }
            sb.AppendLine("=========================systemEnvironmentInfo===========================================");
            AppendModel(nameof(systemEnvironmentInfo), systemEnvironmentInfo);
            sb.AppendLine("==========================cpuInfo==========================================");
            AppendModel(nameof(cpuInfo), cpuInfo);
            sb.AppendLine("===========================gppuInfo=========================================");
            AppendModel(nameof(gppuInfo), gppuInfo);
            sb.AppendLine("===========================motherboardInfo=========================================");
            AppendModel(nameof(motherboardInfo), motherboardInfo);
            sb.AppendLine("===========================RamSummaryInfo=========================================");
            AppendModel(nameof(RamSummaryInfo), RamSummaryInfo);
            sb.AppendLine("===========================DiskInfo=========================================");
            AppendModel(nameof(DiskInfo), DiskInfo);
            sb.AppendLine("==========================NetworkAdapterInfo==========================================");
            AppendModel(nameof(NetworkAdapterInfo), NetworkAdapterInfo);
            sb.AppendLine("==========================RamModuleInfo==========================================");
            AppendModel(nameof(RamModuleInfo), RamModuleInfo);
            sb.AppendLine("==========================OpticalDriveInfo==========================================");
            AppendModel(nameof(OpticalDriveInfo), OpticalDriveInfo);
            sb.AppendLine("=========================monitorInfos===========================================");
            AppendModel(nameof(monitorInfos), monitorInfos);

            return sb.ToString();
        }


    }
}
