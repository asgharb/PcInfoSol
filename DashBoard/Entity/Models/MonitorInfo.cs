using DashBoard.Attributes;
using DashBoard.Entity.Main;

namespace DashBoard.Entity.Models
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
    }
}
