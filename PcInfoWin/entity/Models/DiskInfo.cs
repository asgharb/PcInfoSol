using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;

namespace PcInfoWin.Entity.Models
{
    [Table("DiskInfo")]
    public class DiskInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("DiskInfoID")]
        public int DiskInfoID { get; set; }

        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int SizeGB { get; set; }
        public string Type { get; set; } // HDD, SSD, NVMe
    }
}