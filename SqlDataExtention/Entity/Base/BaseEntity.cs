using System;
using SqlDataExtention.Attributes;


namespace SqlDataExtention.Entity.Base
{
    public class BaseEntity
    {
        [ForeignKey("SystemInfo", "SystemInfoID")]
        public int SystemInfoRef { get; set; }
        public DateTime InsertDate { get; set; } = DateTime.Now;
        public DateTime? ExpireDate { get; set; } = null;
    }
}
