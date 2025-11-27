using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
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
