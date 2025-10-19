using SqlDataExtention.Attributes;
using System;
using System.Linq;
using System.Reflection;


namespace SqlDataExtention.Data
{
    public static class EntityMetadataHelper
    {
        /// <summary>
        /// دریافت نام جدول از Attribute یا خود نام کلاس
        /// </summary>
        public static string GetTableName(Type type)
        {
            return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        }

        /// <summary>
        /// دریافت Property کلید اصلی
        /// </summary>
        public static PropertyInfo GetPrimaryKeyProperty(Type type)
        {
            var prop = type.GetProperties()
                .FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));

            if (prop == null)
                throw new Exception($"[Key] not found in class {type.Name}");

            return prop;
        }

        /// <summary>
        /// دریافت Property کلید خارجی (در خود کلاس یا BaseEntity)
        /// </summary>
        public static PropertyInfo GetForeignKeyProperty(Type type)
        {
            var prop = type.GetProperties()
                .FirstOrDefault(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute)));

            if (prop == null && type.BaseType != null)
                prop = type.BaseType.GetProperties()
                    .FirstOrDefault(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute)));

            if (prop == null)
                throw new Exception($"[ForeignKey] not found in class {type.Name}");

            return prop;
        }

        public static PropertyInfo GetForeignKeyPropertyForParent(PropertyInfo childProp, Type parentType)
        {
            // نوع آیتم زیرمجموعه (مثلاً CpuInfo)
            Type childType = childProp.PropertyType;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(childType) && childType != typeof(string))
                childType = childType.GetGenericArguments().FirstOrDefault(); // برای لیست‌ها

            if (childType == null)
                return null;

            // در کلاس فرزند دنبال ForeignKeyAttribute بگرد که RelatedTable برابر parentType.Name باشد
            var fkProp = childType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(p =>
                {
                    var fkAttr = p.GetCustomAttribute<ForeignKeyAttribute>();
                    return fkAttr != null &&
                           string.Equals(fkAttr.RelatedTable, parentType.Name, StringComparison.OrdinalIgnoreCase);
                });

            return fkProp;
        }



        /// <summary>
        /// دریافت نام ستون از Attribute یا خود PropertyName
        /// </summary>
        public static string GetColumnName(PropertyInfo property)
        {
            return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
        }

        /// <summary>
        /// بررسی اینکه آیا ستون Ignore شده یا خیر
        /// </summary>
        public static bool IsIgnored(PropertyInfo property)
        {
            return Attribute.IsDefined(property, typeof(IgnoreAttribute));
        }

        /// <summary>
        /// بررسی اینکه آیا مقدار در دیتابیس تولید می‌شود (DbGenerated)
        /// </summary>
        public static bool IsDbGenerated(PropertyInfo property)
        {
            return Attribute.IsDefined(property, typeof(DbGeneratedAttribute));
        }
    }
}
