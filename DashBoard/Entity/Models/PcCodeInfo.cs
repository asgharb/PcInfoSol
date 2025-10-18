using DashBoard.Attributes;
using DashBoard.Entity.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashBoard.Entity.Models
{
    public class PcCodeInfo:BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("PcCodeInfoID")]
        public int PcCodeInfoID { get; set; }

        public string PcCodeName { get; set; }

        public string Desc1 {  get; set; }    

        public string Desc2 {  get; set; }

    }
}
