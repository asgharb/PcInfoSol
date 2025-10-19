using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
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
