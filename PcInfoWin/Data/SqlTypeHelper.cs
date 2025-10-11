using System;
using System.Reflection;

namespace PcInfoWin.Data
{

    public static class SqlTypeHelper
    {

        public static string GetSqlType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsEnum) return "INT";
            if (type == typeof(byte)) return "TINYINT";
            if (type == typeof(sbyte)) return "SMALLINT";
            if (type == typeof(short)) return "SMALLINT";
            if (type == typeof(ushort)) return "INT";
            if (type == typeof(int)) return "INT";
            if (type == typeof(uint)) return "BIGINT"; // SQL Server unsigned int ندارد
            if (type == typeof(long)) return "BIGINT";
            if (type == typeof(ulong)) return "DECIMAL(20,0)"; // bigint نمی‌تونه همه مقادیر ulong را نگه دارد
            if (type == typeof(bool)) return "BIT";
            if (type == typeof(string)) return "NVARCHAR(MAX)";
            if (type == typeof(char)) return "NCHAR(1)";
            if (type == typeof(DateTime)) return "DATETIME";
            if (type == typeof(decimal)) return "DECIMAL(18,2)";
            if (type == typeof(float)) return "REAL";
            if (type == typeof(double)) return "FLOAT";
            if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";
            return "NVARCHAR(MAX)";
        }


        public static object ConvertToDbValue(PropertyInfo prop, object value)
        {
            if (value == null) return DBNull.Value;

            Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (type.IsEnum)
                return Convert.ToInt32(value);

            if (type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int))
                return Convert.ToInt32(value);

            if (type == typeof(uint) || type == typeof(long))
                return Convert.ToInt64(value);

            if (type == typeof(ulong))
                return Convert.ToDecimal(value); 

            if (type == typeof(float))
                return Convert.ToSingle(value);

            if (type == typeof(double))
                return Convert.ToDouble(value);

            if (type == typeof(decimal))
                return Convert.ToDecimal(value);

            if (type == typeof(bool))
                return Convert.ToBoolean(value);

            if (type == typeof(char) || type == typeof(string))
                return Convert.ToString(value);

            if (type == typeof(DateTime))
                return Convert.ToDateTime(value);

            if (type == typeof(Guid))
                return (Guid)value;

            // اگر نوع ناشناخته بود، همان value را بفرست
            return value;
        }
    }
}
