using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("GpuInfo")]
    public class GpuInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("GpuInfoID")]
        public int GpuInfoID { get; set; }

        public string Name { get; set; }

        public string Manufacturer { get; set; }

        public string DriverVersion { get; set; }

        public int MemorySizeGB { get; set; }

        public string VideoProcessor { get; set; }

    }
}
