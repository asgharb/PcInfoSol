using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;

namespace PcInfoWin.Entity.Models
{
    [Table("NetworkAdapterInfo")]
    public class NetworkAdapterInfo : BaseEntity
    {
        // کلید اصلی با نام کلاس + ID
        [Key]
        [DbGenerated]
        [Column("NetworkAdapterInfoID")]
        public int NetworkAdapterInfoID { get; set; }

        // فیلدها با نام ستون یکسان
        public string Name { get; set; }

        public string MACAddress { get; set; }

        public bool IsPhysical { get; set; }

        public bool IsEnabled { get; set; }

        public bool IsLAN { get; set; }

    }
}
