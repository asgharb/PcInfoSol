using DashBoard.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DashBoard.Utils
{
    public static class SystemInfoComparer
    {
        public class Difference
        {
            public string Path { get; set; }
            public string Property { get; set; }
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string PrimaryKeyName { get; set; }
            public object PrimaryKeyValue { get; set; }

            public override string ToString()
            {
                return $"{Path}.{Property}: '{Value1}' != '{Value2}' (Key: {PrimaryKeyName}={PrimaryKeyValue})";
            }
        }

        public static List<Difference> CompareSystemInfo(object current, object dbLoaded)
        {
            if (current == null || dbLoaded == null)
                throw new ArgumentNullException("نمونه‌های ورودی نباید null باشند.");

            var diffs = new List<Difference>();

            // فقط property‌هایی که [Ignore] دارند را بررسی کن
            var props = current.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => Attribute.IsDefined(p, typeof(IgnoreAttribute)))
                .ToList();

            foreach (var prop in props)
            {
                var value1 = prop.GetValue(current);
                var value2 = prop.GetValue(dbLoaded);
                string path = prop.Name;

                diffs.AddRange(CompareObjects(value1, value2, path));
            }

            return diffs;
        }

        private static List<Difference> CompareObjects(object obj1, object obj2, string path)
        {
            var diffs = new List<Difference>();
            if (obj1 == null && obj2 == null) return diffs;

            if (obj1 == null || obj2 == null)
            {
                diffs.Add(new Difference
                {
                    Path = path,
                    Property = "(object)",
                    Value1 = obj1?.ToString() ?? "null",
                    Value2 = obj2?.ToString() ?? "null"
                });
                return diffs;
            }

            var type = obj1.GetType();

            // اگر IEnumerable هست (مثل لیست)
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var list1 = ((IEnumerable)obj1).Cast<object>().ToList();
                var list2 = ((IEnumerable)obj2).Cast<object>().ToList();

                int count = Math.Max(list1.Count, list2.Count);
                for (int i = 0; i < count; i++)
                {
                    if (i >= list1.Count || i >= list2.Count)
                    {
                        diffs.Add(new Difference
                        {
                            Path = $"{path}[{i}]",
                            Property = "(missing item)",
                            Value1 = i >= list1.Count ? "null" : "exists",
                            Value2 = i >= list2.Count ? "null" : "exists"
                        });
                        continue;
                    }

                    diffs.AddRange(CompareObjects(list1[i], list2[i], $"{path}[{i}]"));
                }
                return diffs;
            }

            // فیلتر propertyهایی که نباید بررسی شوند
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    !Attribute.IsDefined(p, typeof(KeyAttribute)) &&
                    !Attribute.IsDefined(p, typeof(DbGeneratedAttribute)) &&
                    p.Name != "InsertDate" &&
                    p.Name != "ExpireDate" &&
                    p.Name != "SystemInfoRef")
                .ToList();

            var keyProp = type.GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));

            foreach (var prop in props)
            {
                var v1 = prop.GetValue(obj1);
                var v2 = prop.GetValue(obj2);
                string currentPath = $"{path}.{prop.Name}";

                // اگر نوع ساده است
                if (prop.PropertyType.IsPrimitive ||
                    prop.PropertyType == typeof(string) ||
                    prop.PropertyType == typeof(DateTime) ||
                    prop.PropertyType == typeof(decimal))
                {
                    if (!Equals(v1, v2))
                    {
                        diffs.Add(new Difference
                        {
                            Path = path,
                            Property = prop.Name,
                            Value1 = v1?.ToString(),
                            Value2 = v2?.ToString(),
                            PrimaryKeyName = keyProp?.Name,
                            PrimaryKeyValue = keyProp?.GetValue(obj2)
                        });
                    }
                }
                else
                {
                    // اگر کلاس یا آبجکت تو در تو است
                    diffs.AddRange(CompareObjects(v1, v2, currentPath));
                }
            }

            return diffs;
        }
    }
}
