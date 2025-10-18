using DashBoard.Attributes;
using DashBoard.Entity.Main;

namespace DashBoard.Entity.Models
{
    [Table("GpuInfo")]
    public class GpuInfo : BaseEntity
    {
        // کلید اصلی با نام کلاس + ID
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
