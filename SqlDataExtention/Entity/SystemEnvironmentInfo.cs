using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

namespace SqlDataExtention.Entity
{
    [Table("SystemEnvironmentInfo")]
    public class SystemEnvironmentInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("SystemEnvironmentInfoID")]
        public int SystemEnvironmentInfoID { get; set; }

        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public string Domain { get; set; }
        public string OsVersion { get; set; }
        public bool IsRealVNCInstalled { get; set; }



        public SystemEnvironmentInfo() { }
    }
}
