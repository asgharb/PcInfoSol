using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;

namespace PcInfoWin.Entity.Models
{
    [Table("CpuInfo")]
    public class CpuInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("CpuInfoID")]
        public int CpuInfoID { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
    }
}


