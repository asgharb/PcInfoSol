using System;


namespace SqlDataExtention.Attributes
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; }
        public TableAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public ColumnAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class DbGeneratedAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class CompareAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        public string RelatedTable { get; }
        public string RelatedColumn { get; }
        public ForeignKeyAttribute(string relatedTable, string relatedColumn)
        {
            RelatedTable = relatedTable;
            RelatedColumn = relatedColumn;
        }
    }
}
