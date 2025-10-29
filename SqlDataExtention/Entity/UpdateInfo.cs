using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDataExtention.Entity
{
    public class UpdateInfo : BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("UpdateInfoID")]
        public int UpdateInfoID { get; set; }
        public string UpdatePath { get; set; }
        public UpdateInfo() { }
        public UpdateInfo(string path) { UpdatePath = path; }

    }
}
