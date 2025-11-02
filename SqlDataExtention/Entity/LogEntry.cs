using SqlDataExtention.Attributes;
using System;


namespace SqlDataExtention.Entity
{
    [Table("Log")]
    public class LogEntry
    {
        public int LogID { get; set; }
        public DateTime LogDate { get; set; }
        public int SysId { get; set; }
        public string MachineName { get; set; }
        public string FormName { get; set; }
        public string MethodName { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string InnerMessage { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
