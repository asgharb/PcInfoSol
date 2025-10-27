using ClosedXML.Excel;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;


namespace DashBoard
{
    public static class ExcelExporter
    {
        // 🔹 مسیر قالب
        private const string TemplateFile = "Template_With_Macros.xlsm";

        /// <summary>
        /// خروجی کامل Master–Details از لیست SystemInfo به اکسل (با لینک به جزئیات)
        /// </summary>
        public static void ExportSystemInfoToExcel(List<SystemInfo> allSystems)
        {
            if (allSystems == null || allSystems.Count == 0)
                throw new InvalidOperationException("لیست سیستم‌ها خالی است.");

            if (!File.Exists(TemplateFile))
                throw new FileNotFoundException("فایل قالب ماکرو پیدا نشد.", TemplateFile);

            var outputPath = $"SystemInfo_MasterDetail_{DateTime.Now:yyyyMMdd_HHmmss}.xlsm";

            using (var workbook = new XLWorkbook(TemplateFile))
            {
                // ===== شیت اصلی =====
                var dtMain = BuildMainDataTable(allSystems);

                if (workbook.Worksheets.Contains("SystemInfo"))
                    workbook.Worksheet("SystemInfo").Delete();

                var wsMain = workbook.Worksheets.Add(dtMain, "SystemInfo");
                wsMain.Columns().AdjustToContents();

                int baseColumn = dtMain.Columns.Count + 1;

                // ===== اضافه کردن شیت‌های جزئی =====
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "CpuInfo", s => ToList(s.cpuInfo));
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "GpuInfo", s => ToList(s.gpuInfo));
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "RamSummaryInfo", s => ToList(s.RamSummaryInfo));
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "RamModuleInfo", s => s.RamModuleInfo);
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "NetworkAdapterInfo", s => s.NetworkAdapterInfo);
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "DiskInfo", s => s.DiskInfo);
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "MonitorInfo", s => s.monitorInfo);
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "OpticalDriveInfo", s => s.OpticalDriveInfo);
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "MotherboardInfo", s => ToList(s.motherboardInfo));
                AddDetailSheet(workbook, wsMain, allSystems, ref baseColumn, "SystemEnvironmentInfo", s => ToList(s.systemEnvironmentInfo));

                wsMain.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }

            // باز کردن خروجی در Excel
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });
        }

        // 🔹 ساخت جدول اصلی از SystemInfo
        private static DataTable BuildMainDataTable(List<SystemInfo> systems)
        {
            var table = new DataTable("SystemInfo");
            table.Columns.Add("RowNumber", typeof(int));
            table.Columns.Add("SystemInfoID", typeof(int));
            table.Columns.Add("PcCode");
            table.Columns.Add("IpAddress");
            table.Columns.Add("UserFullName");
            table.Columns.Add("PersonnelCode");
            table.Columns.Add("Unit");
            table.Columns.Add("InsertDate", typeof(DateTime));
            table.Columns.Add("ExpireDate", typeof(DateTime));

            int row = 1;
            foreach (var s in systems)
            {
                table.Rows.Add(
                    row++,
                    s.SystemInfoID,
                    GetSafe(s.pcCodeInfo, x => x.PcCode),
                    s.NetworkAdapterInfo?.FirstOrDefault(a => !string.IsNullOrEmpty(a.IpAddress))?.IpAddress,
                    GetSafe(s.pcCodeInfo, x => x.UserFullName),
                    GetSafe(s.pcCodeInfo, x => x.PersonnelCode.ToString()),
                    GetSafe(s.pcCodeInfo, x => x.Unit),
                    s.InsertDate,
                   s.ExpireDate.HasValue ? (object)s.ExpireDate.Value : DBNull.Value
                );
            }

            return table;
        }

        // 🔹 اضافه کردن هر شیت جزئی
        private static void AddDetailSheet<T>(
            XLWorkbook workbook,
            IXLWorksheet wsMain,
            List<SystemInfo> allSystems,
            ref int columnIndex,
            string sheetName,
            Func<SystemInfo, IEnumerable<T>> selector)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (var s in allSystems)
            {
                var items = selector(s) ?? Enumerable.Empty<T>();
                foreach (var it in items)
                    rows.Add(ToFlatDict(it, s.SystemInfoID));
            }

            if (!rows.Any())
                return;

            var dt = ToDataTable(rows);
            if (workbook.Worksheets.Contains(sheetName))
                workbook.Worksheet(sheetName).Delete();

            var ws = workbook.Worksheets.Add(dt, sheetName);
            ws.Columns().AdjustToContents();

            // لینک در شیت اصلی
            // لینک در شیت اصلی
            wsMain.Cell(1, columnIndex).Value = sheetName;
            int dataRowCount = wsMain.RowsUsed().Count() - 1; // header 제외
            for (int i = 0; i < dataRowCount; i++)
            {
                int systemId = Convert.ToInt32(wsMain.Cell(i + 2, 2).Value); // ستون 2 = SystemInfoID
                var cell = wsMain.Cell(i + 2, columnIndex);

                // ایجاد فرمول HYPERLINK — وقتی کلیک بشه به شیت مربوطه میره
                string subAddress = $"#'{sheetName}'!A1";
                // فرمول: =HYPERLINK("#'Sheet'!A1", "مشاهده جزئیات")
                cell.FormulaA1 = $"=HYPERLINK(\"{subAddress}\",\"مشاهده جزئیات\")";

                // استایل لینک آبی و زیرخط
                cell.Style.Font.FontColor = XLColor.Blue;
                cell.Style.Font.Underline = XLFontUnderlineValues.Single;

                // (نیاز به کامنت نیست — ID از ستون 2 خوانده می‌شود)
            }

            columnIndex++;
        }

        // 🔹 دیکشنری‌سازی ساده از آبجکت‌ها
        private static Dictionary<string, object> ToFlatDict(object entity, int systemId)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["SystemInfoID"] = systemId
            };

            if (entity == null) return dict;

            foreach (var p in entity.GetType().GetProperties())
            {
                try
                {
                    var val = p.GetValue(entity);
                    dict[p.Name] = val ?? DBNull.Value;
                }
                catch { dict[p.Name] = DBNull.Value; }
            }

            return dict;
        }

        // 🔹 تبدیل لیست دیکشنری به DataTable
        private static DataTable ToDataTable(List<Dictionary<string, object>> rows)
        {
            var dt = new DataTable();
            if (!rows.Any()) return dt;

            foreach (var col in rows.SelectMany(r => r.Keys).Distinct())
                dt.Columns.Add(col);

            foreach (var r in rows)
            {
                var dr = dt.NewRow();
                foreach (var kv in r)
                    dr[kv.Key] = kv.Value ?? DBNull.Value;
                dt.Rows.Add(dr);
            }

            return dt;
        }

        // 🔹 ابزارهای کمکی
        private static List<T> ToList<T>(T obj) =>
            obj == null ? new List<T>() : new List<T> { obj };

        private static string GetSafe<T>(List<T> list, Func<T, string> selector)
        {
            if (list == null || list.Count == 0) return "-";
            try { return selector(list.Last()) ?? "-"; } catch { return "-"; }
        }
    }
}