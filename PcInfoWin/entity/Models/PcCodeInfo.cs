using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Entity.Models
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

        //public PcCodeInfo()
        //{
        //    PcCodeInfoID = 0; PcCodeName=""; Desc1=""; Desc2="";
        //}
    }
}
