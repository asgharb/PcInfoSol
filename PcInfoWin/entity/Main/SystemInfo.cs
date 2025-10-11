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

        public static SystemInfo GetSystemInfoWithChildrenFromDB(int sysId)
        {
            var db = new DataHelper();

            var sysInfo = db.SelectById<SystemInfo>(sysId);

            if (sysInfo == null) return null;

            // RAM Summary
            sysInfo.RamSummaryInfo = db.SelectByColumns<RamSummaryInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } }).FirstOrDefault();

            // RAM Modules
            sysInfo.RamModuleInfo = db.SelectByColumns<RamModuleInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } });

            // Disks
            sysInfo.DiskInfo = db.SelectByColumns<DiskInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } });

            // Network Adapters
            sysInfo.NetworkAdapterInfo = db.SelectByColumns<NetworkAdapterInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } });

            // Optical Drives
            sysInfo.OpticalDriveInfo = db.SelectByColumns<OpticalDriveInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } });

            // Monitors
            sysInfo.monitorInfos = db.SelectByColumns<MonitorInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } });

            // CPU
            sysInfo.cpuInfo = db.SelectByColumns<CpuInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } }).FirstOrDefault();
            //Gpu
            sysInfo.gppuInfo = db.SelectByColumns<GpuInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } }).FirstOrDefault();

            //Motherboard
            sysInfo.motherboardInfo = db.SelectByColumns<MotherboardInfo>(
                new Dictionary<string, object> { { "SystemInfoRef", sysId } }).FirstOrDefault();

            return sysInfo;
        }

        public static int InserNewInfoToDB()
        {
            try
            {
                var db = new DataHelper();

                // 1. ساخت SystemInfo
                var sysInfo = new SystemInfo();

                // 2. پر کردن اطلاعات سخت‌افزار
                sysInfo.cpuInfo = new CpuInfoProvider().GetCpuInfo();
                sysInfo.gppuInfo = new GpuInfoProvider().GetGpuInfo();
                sysInfo.motherboardInfo = new MotherboardInfoProvider().GetMotherboardInfo();
                sysInfo.RamSummaryInfo = new RamInfoProvider().GetRamSummary();
                sysInfo.RamModuleInfo = new RamInfoProvider().GetRamModules();
                sysInfo.DiskInfo = new DiskInfoProvider().GetDisks();
                sysInfo.OpticalDriveInfo = new OpticalDriveInfoProvider().GetAllOpticalDrives();
                sysInfo.monitorInfos = new MonitorInfoProvider().GetAllMonitors();
                sysInfo.NetworkAdapterInfo = new NetworkAdapterInfoProvider().GetAllNetworkAdapters();
                sysInfo.systemEnvironmentInfo = new SystemEnvironmentProvider().GetSystemEnvironmentInfo();

                // 3. درج SystemInfo و گرفتن ID
                int sysId = (int)db.InsertAutoKey(sysInfo);
                Console.WriteLine($"SystemInfoID: {sysId}");

                // 4. درج CPU
                if (sysInfo.cpuInfo != null)
                {
                    sysInfo.cpuInfo.SystemInfoRef = sysId;
                    db.Insert(sysInfo.cpuInfo);
                }

                // 5. درج GPU
                if (sysInfo.gppuInfo != null)
                {
                    sysInfo.gppuInfo.SystemInfoRef = sysId;
                    db.Insert(sysInfo.gppuInfo);
                }

                // 6. درج Motherboard
                if (sysInfo.motherboardInfo != null)
                {
                    sysInfo.motherboardInfo.SystemInfoRef = sysId;
                    db.Insert(sysInfo.motherboardInfo);
                }

                // 7. درج RAM Summary
                if (sysInfo.RamSummaryInfo != null)
                {
                    sysInfo.RamSummaryInfo.SystemInfoRef = sysId;
                    db.Insert(sysInfo.RamSummaryInfo);
                }

                // 8. درج RAM Modules
                if (sysInfo.RamModuleInfo != null && sysInfo.RamModuleInfo.Count > 0)
                {
                    foreach (var ram in sysInfo.RamModuleInfo)
                    {
                        ram.SystemInfoRef = sysId;
                    }
                    db.BulkInsert(sysInfo.RamModuleInfo);
                }

                // 9. درج Disk ها
                if (sysInfo.DiskInfo != null && sysInfo.DiskInfo.Count > 0)
                {
                    foreach (var disk in sysInfo.DiskInfo)
                    {
                        disk.SystemInfoRef = sysId;
                    }
                    db.BulkInsert(sysInfo.DiskInfo);
                }

                // 10. درج Optical Drives
                if (sysInfo.OpticalDriveInfo != null && sysInfo.OpticalDriveInfo.Count > 0)
                {
                    foreach (var od in sysInfo.OpticalDriveInfo)
                        od.SystemInfoRef = sysId;
                    db.BulkInsert(sysInfo.OpticalDriveInfo);
                }

                // 11. درج Monitors
                if (sysInfo.monitorInfos != null && sysInfo.monitorInfos.Count > 0)
                {
                    foreach (var mon in sysInfo.monitorInfos)
                        mon.SystemInfoRef = sysId;
                    db.BulkInsert(sysInfo.monitorInfos);
                }

                // 12. درج Network Adapters
                if (sysInfo.NetworkAdapterInfo != null && sysInfo.NetworkAdapterInfo.Count > 0)
                {
                    foreach (var nic in sysInfo.NetworkAdapterInfo)
                        nic.SystemInfoRef = sysId;
                    db.BulkInsert(sysInfo.NetworkAdapterInfo);
                }

                if (sysInfo.systemEnvironmentInfo != null )
                {
                    sysInfo.systemEnvironmentInfo.SystemInfoRef = sysId;
                    db.Insert(sysInfo.systemEnvironmentInfo);
                }

                return sysId;
            }
            catch (Exception)
            {
                return -1;
            }
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
