using PcInfoWin.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Data
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
            // بررسی همه propertyهای والد و کلاس‌های base
            var props = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var prop in props)
            {
                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                {
                    // مقایسه نام کلاس child با RelatedTable
                    if (string.Equals(fkAttr.RelatedTable, childProp.PropertyType.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return prop;
                    }
                }
            }

            // اگر پیدا نشد null برمی‌گردد
            return null;
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
