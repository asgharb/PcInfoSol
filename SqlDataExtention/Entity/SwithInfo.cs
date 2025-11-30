using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("SwithInfo")]
    public class SwithInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("SwithInfoID")]
        public int SwithInfoID { get; set; }

        public string SwitchIp { get; set; }
        public string SwitchPort { get; set; }

        public string PcMac { get; set; }
        public string PcVlan { get; set; }
        public string PcIp { get; set; }

        public string PhoneMac { get; set; }
        public string PhoneVlan { get; set; }
        public string PhoneIp { get; set; }

        public string UserFullName { get; set; }
    }
}



//using SqlDataExtention.Attributes;
//using SqlDataExtention.Entity.Base;

//namespace SqlDataExtention.Entity
//{
//    [Table("SwithInfo")]
//    public class SwithInfo : BaseEntity
//    {
//        [Key]
//        [DbGenerated]
//        [Column("SwithInfoID")]
//        public int SwithInfoID { get; set; }
//        public string Mac { get; set; }
//        public string FoundSwitch { get; set; }
//        public string FoundPort { get; set; }
//        public string Vlan { get; set; }

//        public string PhoneMac { get; set; }
//        public string PhoneVlan { get; set; }
//        public string PhoneIp { get; set; }



//        public string Status { get; set; }
//    }

//}
