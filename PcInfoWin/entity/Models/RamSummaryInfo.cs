using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;

namespace PcInfoWin.Entity.Models
{
    [Table("RamSummaryInfo")]
    public class RamSummaryInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("RamSummaryInfoID")]
        public int RamSummaryInfoID { get; set; }

        public int TotalModules { get; set; }

        public int TotalCapacityGB { get; set; }


    }
}
