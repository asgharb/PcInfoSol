using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;

namespace PcInfoWin.Entity.Models
{
    [Table("RamModuleInfo")]
    public class RamModuleInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("RamModuleInfoID")]
        public int RamModuleInfoID { get; set; }

        // ستون‌ها
        public string Manufacturer { get; set; }

        public int CapacityGB { get; set; }  // ظرفیت بر حسب مگابایت

        public int SpeedMHz { get; set; }     // سرعت

        public string MemoryType { get; set; } // DDR3, DDR4, DDR5, Unknown

 
    }
}
