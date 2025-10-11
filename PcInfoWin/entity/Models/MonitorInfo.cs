using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;
using System;
namespace PcInfoWin.Entity.Models
{
    [Table("MonitorInfo")]
    public class MonitorInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("MonitorInfoID")]
        public int MonitorInfoID { get; set; }


        public string Model { get; set; }     // from WmiMonitorID
        public string Manufacturer { get; set; }         // from WmiMonitorID
        public string ProductCodeID { get; set; }        // product code (if available)
        public string PublicSerialNumber { get; set; }
        public double SizeInInches { get; set; }

        // ستون‌ها
        //public string DisplayName { get; set; }           // \\.\DISPLAY1 from Screen
        //[Ignore]
        //public string InstanceName { get; set; }         // WMI instance name (if any)
        //[Ignore]
        //public int Width { get; set; }
        //[Ignore]
        //public int Height { get; set; }
        //[Ignore]
        //public int PhysicalWidthMm { get; set; }
        //[Ignore]
        //public int PhysicalHeightMm { get; set; }

    }
}
