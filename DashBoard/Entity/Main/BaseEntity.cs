using DashBoard.Attributes;
using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace DashBoard.Entity.Main
{

    public abstract class BaseEntity
    {
        // کلید خارجی به جدول SystemInfo
        [ForeignKey("SystemInfo", "SystemInfoID")]
        public int SystemInfoRef { get; set; }


        public DateTime InsertDate { get; set; }
        public DateTime? ExpireDate { get; set; }


        public override string ToString()
        {
            var sb = new StringBuilder();
            var props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                object value = prop.GetValue(this, null) ?? "null";

                // اگر لیست یا IEnumerable باشد، محتویات را هم چاپ کن
                if (value is IEnumerable enumerable && !(value is string))
                {
                    sb.AppendLine($"{prop.Name}:");
                    foreach (var item in enumerable)
                    {
                        sb.AppendLine("  - " + (item?.ToString() ?? "null"));
                    }
                }
                else
                {
                    sb.AppendLine($"{prop.Name}: {value}");
                }
            }

            return sb.ToString();
        }
    }
}
