using SqlDataExtention.Attributes;
using SqlDataExtention.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SqlDataExtention.Entity.Main
{
    public class SystemInfo
    {
        [Key]
        [DbGenerated]
        [Column("SystemInfoID")]
        public int SystemInfoID { get; set; }
        [Ignore]
        public SystemEnvironmentInfo systemEnvironmentInfo { get; set; }
        [Ignore]
        public List<PcCodeInfo> pcCodeInfo { get; set; } 
        [Ignore]
        public CpuInfo cpuInfo { get; set; } 
        [Ignore]
        public GpuInfo gpuInfo { get; set; } = new GpuInfo();
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
        public List<MonitorInfo> monitorInfo { get; set; } 

        public DateTime InsertDate { get; set; } = DateTime.Now;
        public DateTime? ExpireDate { get; set; }

        //}
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
            AppendModel(nameof(gpuInfo), gpuInfo);
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
            AppendModel(nameof(monitorInfo), monitorInfo);
            sb.AppendLine("=========================PcCodeInfo===========================================");
            AppendModel(nameof(pcCodeInfo), pcCodeInfo);

            return sb.ToString();
        }

    }

}
