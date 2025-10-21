using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Base;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDataExtention.Entity
{
    public class PcCodeInfo:BaseEntity
    {
        [Key]
        [DbGenerated]
        [Column("PcCodeInfoID")]
        public int PcCodeInfoID { get; set; }

        public string PcCode { get; set; }

        public string UserFullName {  get; set; }    

        public int PersonnelCode {  get; set; }

        public string Unit {  get; set; }

        public string Desc1 {  get; set; }

        public string Desc2 {  get; set; }

        public string Desc3 {  get; set; }

    }
}
