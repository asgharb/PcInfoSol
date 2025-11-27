using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("NetworkAdapterInfo")]
    public class NetworkAdapterInfo : BaseEntity
    {
        // کلید اصلی با نام کلاس + ID
        [Key]
        [DbGenerated]
        [Column("NetworkAdapterInfoID")]
        public int NetworkAdapterInfoID { get; set; }

        public string Name { get; set; }
        public string MACAddress { get; set; }
        public string IpAddress { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsLAN { get; set; }
    }
}