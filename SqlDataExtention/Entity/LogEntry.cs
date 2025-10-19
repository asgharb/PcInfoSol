using SqlDataExtention.Attributes;


namespace SqlDataExtention.Entity
{
    [Table("Log")]
    public class LogEntry
    {
        [Key]
        [DbGenerated]
        [Column("LogID")]
        public int LogID { get; set; }

        [Column("EntityName")]
        public string EntityName { get; set; }

        [Column("ActionType")]
        public string ActionType { get; set; }

        [Column("PrimaryKeyValue")]
        public string PrimaryKeyValue { get; set; }

        [Column("Message")]
        public string Message { get; set; }

        [Column("CreatedAt")]
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;
    }
}
