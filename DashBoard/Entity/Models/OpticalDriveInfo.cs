using DashBoard.Attributes;
using DashBoard.Entity.Main;

namespace DashBoard.Entity.Models
{
    [Table("OpticalDriveInfo")]
    public class OpticalDriveInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("OpticalDriveInfoID")]
        public int OpticalDriveInfoID { get; set; }

        // ستون‌ها
        public string Name { get; set; }

        public string Manufacturer { get; set; }

        public string MediaType { get; set; }

    }
}
