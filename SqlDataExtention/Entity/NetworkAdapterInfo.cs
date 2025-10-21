using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;
using System.Collections.Generic;
using System.Linq;


namespace SqlDataExtention.Entity
{
    [Table("NetworkAdapterInfo")]
    public class NetworkAdapterInfo : BaseEntity
    {
        // کلید اصلی با نام کلاس + ID
        [Key]
        [DbGenerated]
        [Column("NetworkAdapterInfoID")]
        public int NetworkAdapterInfoID { get; set; }

        public string Name { get; set; }
        public string MACAddress { get; set; }
        public string IpAddress { get; set; }
        public bool IsPhysical { get; set; }
        public bool IsMotherboardLan { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsLAN { get; set; }


        public string GetPriorityIp(List<NetworkAdapterInfo> adapters)
        {
            if (adapters == null || adapters.Count == 0) return null;

            // اولویت: LAN فعال با IP معتبر
            var ip = adapters
                .Where(a => a.IsLAN && a.IsEnabled && !string.IsNullOrWhiteSpace(a.IpAddress))
                .OrderByDescending(a => a.IsMotherboardLan) // مادربرد اول
                .Select(a => a.IpAddress)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(ip))
                return ip;

            // دومین LAN (غیر فعال یا LAN دوم با IP معتبر)
            ip = adapters
                .Where(a => a.IsLAN && !string.IsNullOrWhiteSpace(a.IpAddress))
                .OrderByDescending(a => a.IsMotherboardLan) // مادربرد اول
                .Select(a => a.IpAddress)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(ip))
                return ip;

            // Wi-Fi فعال با IP معتبر
            ip = adapters
                .Where(a => !a.IsLAN && a.IsEnabled && !string.IsNullOrWhiteSpace(a.IpAddress))
                .Select(a => a.IpAddress)
                .FirstOrDefault();

            return ip; // ممکن است null باشد اگر هیچ IP معتبری نباشد
        }

    }
}