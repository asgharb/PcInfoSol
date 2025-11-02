using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace DashBoard.Extention
{
    public static class Utils
    {
        /// <summary>
        /// تبدیل یک آبجکت تکی به DTO دینامیک بدون فیلد ClassNameId
        /// </summary>
        public static dynamic ToDto<T>(T obj)
        {
            if (obj == null)
                return new ExpandoObject();

            var type = typeof(T);
            var idName = type.Name + "Id";

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !string.Equals(p.Name, idName, StringComparison.OrdinalIgnoreCase))
                            .ToList();

            IDictionary<string, object> dto = new ExpandoObject();

            foreach (var p in props)
            {
                try { dto[p.Name] = p.GetValue(obj); }
                catch { dto[p.Name] = null; }
            }

            return dto;
        }

        /// <summary>
        /// تبدیل یک لیست از آبجکت‌ها به List<dynamic> بدون فیلد Id
        /// </summary>
        public static List<dynamic> ToDtoList<T>(IEnumerable<T> list)
        {
            if (list == null || !list.Any())
                return new List<dynamic>();

            var result = new List<dynamic>();

            foreach (var item in list)
            {
                result.Add(ToDto(item));
            }

            return result;
        }

        /// <summary>
        /// کمکی برای یک آبجکت تکی یا null، تبدیل به لیست یک‌تایی دینامیک
        /// </summary>
        public static List<dynamic> ToDtoListSingle<T>(T obj)
        {
            if (obj == null)
                return new List<dynamic>();
            return new List<dynamic> { ToDto(obj) };
        }
    }
}
