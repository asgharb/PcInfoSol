using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("MotherboardInfo")]
    public class MotherboardInfo : BaseEntity
    {
        // کلید اصلی
        [Key]
        [DbGenerated]
        [Column("MotherboardInfoID")]
        public int MotherboardInfoID { get; set; }
        public string Manufacturer { get; set; }
        public string Product { get; set; }
        public string SerialNumber { get; set; }
        public int TotalRamSlots { get; set; }   // تعداد کل اسلات RAM
        public int UsedRamSlots { get; set; }    // تعداد اسلات‌های پر شده


    }
}
