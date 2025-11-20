using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("RamModuleInfo")]
    public class RamModuleInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("RamModuleInfoID")]
        public int RamModuleInfoID { get; set; }

        public string Manufacturer { get; set; }

        public int CapacityGB { get; set; }  

        public int SpeedMHz { get; set; }    

        public string MemoryType { get; set; } // DDR3, DDR4, DDR5, Unknown

    }
}
