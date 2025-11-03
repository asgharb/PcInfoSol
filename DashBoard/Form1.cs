using ClosedXML.Excel;
using DashBoard.Data;
using DashBoard.Extention;
using System.ServiceProcess;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;


namespace DashBoard
{
    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {

        List<SystemInfo> allSystems;


        private List<string> editableColumns = new List<string> { "PcCode", "UserFullName", "PersonnelCode", "unit", "Desc1", "Desc2", "Desc3", "Desc4", "Desc5", "Desc6", "Desc7" };
        private readonly Dictionary<string, string> columnDisplayNames = new Dictionary<string, string>
{
    { "PcCode", "کد سیستم (PcCode)" },
    { "UserFullName", "نام کاربر (UserFullName)" },
    { "PersonnelCode", "کد پرسنلی (PersonnelCode)" },
    { "Desc1", "(Desc1)" },
    { "Desc2", "(Desc2)" },
    { "Desc3", "(Desc3)" },
    { "Desc4", "(Desc4)" },
    { "Desc5", "(Desc5)" },
    { "Desc6", "(Desc6)" },
    { "Desc7", "(Desc7)" },
    { "unit", "(واحد)" }
};
        // master view reference
        private GridView masterView;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            initGridControl();
        }

        private void initGridControl()
        {
            gridControl1.DataSourceChanged += gridControl1_DataSourceChanged;

            loadGrid();

            gridControl1.UseEmbeddedNavigator = true;
            ControlNavigator navigator = gridControl1.EmbeddedNavigator;
            navigator.Buttons.BeginUpdate();
            try
            {
                navigator.Buttons.Append.Visible = false;
                navigator.Buttons.Remove.Visible = false;
            }
            finally
            {
                navigator.Buttons.EndUpdate();
            }

            SetupGridForPcCodeEditing();
            gridView1.DoubleClick += gridView1_DoubleClick;
        }

        private void loadGrid()
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                gridControl1.DataSource = null;
                gridControl1.DataMember = null;

                DataSet ds = new DataSet();

                var helper = new DataSelectHelperNoFilter();
                allSystems = helper.SelectAllFullSystemInfo();


                var transformedSystems = allSystems
                    .Select((s, index) => new
                    {
                        No = index + 1,   // شماره ردیف خودکار (شروع از 1)
                        SystemInfoID = s.SystemInfoID,
                        PcCode = GetSafeDesc(s.pcCodeInfo, x => x.PcCode),
                        IpAddress = s.NetworkAdapterInfo != null
                                    ? s.NetworkAdapterInfo
                                        .Where(a => !string.IsNullOrWhiteSpace(a.IpAddress))
                                        .OrderByDescending(a => a.IsLAN)
                                        .ThenByDescending(a => a.IsEnabled)
                                        .Select(a => a.IpAddress.Trim())
                                        .FirstOrDefault()
                                    : null,
                        UserFullName = GetSafeDesc(s.pcCodeInfo, x => x.UserFullName),
                        PersonnelCode = GetSafeDesc(s.pcCodeInfo, x => x.PersonnelCode.ToString()),
                        Unit = GetSafeDesc(s.pcCodeInfo, x => x.Unit),
                        Desc1 = GetSafeDesc(s.pcCodeInfo, x => x.Desc1),
                        Desc2 = GetSafeDesc(s.pcCodeInfo, x => x.Desc2),
                        Desc3 = GetSafeDesc(s.pcCodeInfo, x => x.Desc3),
                        Desc4 = GetSafeDesc(s.pcCodeInfo, x => x.Desc4),
                        Desc5 = GetSafeDesc(s.pcCodeInfo, x => x.Desc5),
                        Desc6 = GetSafeDesc(s.pcCodeInfo, x => x.Desc6),
                        Desc7 = GetSafeDesc(s.pcCodeInfo, x => x.Desc7),
                        VNC = s.systemEnvironmentInfo.IsRealVNCInstalled,
                        InsertDate = s.InsertDate,
                        pcCodeInfo = Utils.ToDtoList(s.pcCodeInfo),
                        systemEnvironmentInfo = Utils.ToDtoListSingle(s.systemEnvironmentInfo),
                        RamSummaryInfo = Utils.ToDtoListSingle(s.RamSummaryInfo),
                        RamModuleInfo = Utils.ToDtoList(s.RamModuleInfo),
                        cpuInfo = Utils.ToDtoListSingle(s.cpuInfo),
                        gpuInfo = Utils.ToDtoListSingle(s.gpuInfo),
                        DiskInfo = Utils.ToDtoList(s.DiskInfo),
                        NetworkAdapterInfo = Utils.ToDtoList(s.NetworkAdapterInfo),
                        monitorInfo = Utils.ToDtoList(s.monitorInfo),
                        motherboardInfo = Utils.ToDtoListSingle(s.motherboardInfo),
                        OpticalDriveInfo = Utils.ToDtoList(s.OpticalDriveInfo),

                    })
                    .ToList();



                DataTable dtSystemInfo = ToDataTable(transformedSystems);
                dtSystemInfo.TableName = "SystemInfo";
                //dtSystemInfo.Columns.Add("VNCConnect", typeof(string));
                ds.Tables.Add(dtSystemInfo);

                gridControl1.DataSource = ds;
                gridControl1.DataMember = "SystemInfo";


                if (gridView1.Columns["VNCConnect"] == null)
                {
                    var btnVNC = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
                    btnVNC.Buttons[0].Caption = "اتصال";
                    btnVNC.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                    btnVNC.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;


                    var colVNC = gridView1.Columns.AddVisible("VNCConnect", "اتصال VNC");
                    colVNC.ColumnEdit = btnVNC;
                    colVNC.Width = 100;

                    Image original = Properties.Resources.vnc_3;

                    // تغییر اندازه (مثلا 16x16 یا 24x24)
                    Image resized = new Bitmap(original, new Size(48 * 3, 32));

                    // اختصاص به دکمه
                    btnVNC.Buttons[0].ImageOptions.Image = resized;

                    gridControl1.RepositoryItems.Add(btnVNC);
                    colVNC.OptionsColumn.AllowEdit = true;
                    gridView1.OptionsBehavior.Editable = true;
                }


                gridView1.RowHeight = 35;
                gridView1.Columns["SystemInfoID"].Visible = false;

                gridView1.RowStyle -= gridView1_RowStyle;
                gridView1.RowStyle += gridView1_RowStyle;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در بارگذاری داده‌ها: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        void gridView1_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? Color.LightGray : Color.WhiteSmoke;
        }

        private string GetSafeDesc(IList<PcCodeInfo> list, Func<PcCodeInfo, string> selector)
        {
            if (list == null || list.Count == 0)
                return "-";

            var value = selector(list.Last());
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        void gridControl1_DataSourceChanged(object sender, EventArgs e)
        {
            gridControl1.MainView.PopulateColumns();
            (gridControl1.MainView as GridView).BestFitColumns();
        }

        private void SetupGridForPcCodeEditing()
        {
            // 1) Force initialize so views/columns are created
            gridControl1.ForceInitialize();

            // 2) identify master view
            masterView = gridControl1.MainView as GridView;
            if (masterView == null) return;

            // 3) Make master view editable in-place (we will control per-column editability)
            masterView.OptionsBehavior.Editable = true;
            masterView.OptionsBehavior.EditingMode = GridEditingMode.Inplace;

            // 4) Ensure DataTable exists
            var ds = gridControl1.DataSource as DataSet;
            var dt = ds?.Tables["SystemInfo"];
            if (dt == null) return;

            // 5) ابتدا همه ستون‌ها را غیرقابل ویرایش کن (ظاهر سفید)
            masterView.BeginUpdate();
            try
            {
                foreach (GridColumn col in masterView.Columns)
                {
                    col.OptionsColumn.AllowEdit = false;
                    //col.AppearanceCell.BackColor = Color.White;
                }
            }
            finally { masterView.EndUpdate(); }

            // 6) Ensure detail views (existing ones) are non-editable
            foreach (BaseView baseView in gridControl1.Views)
            {
                if (baseView is GridView gv && gv != masterView)
                {
                    DisableEditingOnView(gv);
                }
            }

            // مثال: فقط ستون "PersonnelCode" عددی باشد
            if (masterView.Columns["PersonnelCode"] != null)
            {
                RepositoryItemTextEdit numericEditor = new RepositoryItemTextEdit();
                numericEditor.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
                numericEditor.Mask.EditMask = "\\d+"; // فقط اعداد مجازند
                numericEditor.Mask.UseMaskAsDisplayFormat = true;

                // جلوگیری از ورود کاراکتر غیرعددی
                numericEditor.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true; // رد ورودی غیرعددی
                    }
                };

                // اعمال این ادیتور روی ستون
                masterView.Columns["PersonnelCode"].ColumnEdit = numericEditor;
            }


            // 7) Listen for detail views that get registered later (on-demand)
            gridControl1.ViewRegistered -= GridControl1_ViewRegistered;
            gridControl1.ViewRegistered += GridControl1_ViewRegistered;

            // 8) Ensure only editableColumns open editor in master (ShowingEditor)
            masterView.ShowingEditor -= MasterView_ShowingEditor;
            masterView.ShowingEditor += MasterView_ShowingEditor;

            // 9) Handle value changed on master for editable columns
            masterView.CellValueChanged -= MasterView_CellValueChanged;
            masterView.CellValueChanged += MasterView_CellValueChanged;

            // 10) اعمال لیست editableColumns و تنظیم ReadOnly در DataTable & GridColumn
            SetEditableColumns(editableColumns);
        }

        private void SetEditableColumns(List<string> columns)
        {
            if (columns == null) columns = new List<string>();
            editableColumns = columns;

            if (masterView == null) return;

            masterView.BeginUpdate();
            try
            {
                // 1) DataTable: ستون‌های غیرقابل ویرایش = ReadOnly = true
                var ds = gridControl1.DataSource as DataSet;
                var dt = ds?.Tables["SystemInfo"];
                if (dt != null)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        dc.ReadOnly = !editableColumns.Contains(dc.ColumnName);
                    }
                }

                // 2) GridView: تنظیم AllowEdit و رنگ‌دهی مناسب
                foreach (GridColumn col in masterView.Columns)
                {
                    bool allow = editableColumns.Contains(col.FieldName);
                    col.OptionsColumn.AllowEdit = allow;
                    //col.AppearanceCell.BackColor = allow ? Color.LightYellow : Color.White;
                }
            }
            finally
            {
                masterView.EndUpdate();
            }
        }

        private void DisableEditingOnView(GridView gv)
        {
            if (gv == null) return;
            gv.OptionsBehavior.Editable = false;

            foreach (GridColumn c in gv.Columns)
            {
                c.OptionsColumn.AllowEdit = false;
                c.AppearanceCell.BackColor = Color.White;
            }
            if (gv.Columns["PcCode"] != null)
                gv.Columns["PcCode"].OptionsColumn.AllowEdit = false;
        }


        private void GridControl1_ViewRegistered(object sender, ViewOperationEventArgs e)
        {
            if (e.View is GridView gv)
            {
                if (gv != masterView)
                    DisableEditingOnView(gv);
                else
                    SetEditableColumns(editableColumns); // اگر master دوباره رجیستر شد، ستون‌ها را مجدد اعمال کن
            }
        }


        private void MasterView_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var view = sender as GridView;
            if (view == null || view.FocusedColumn == null) return;

            if (!editableColumns.Contains(view.FocusedColumn.FieldName))
                e.Cancel = true;
        }

        private bool suppressCellValueChanged = false;

        private void MasterView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (suppressCellValueChanged) return;

            try
            {
                if (!editableColumns.Contains(e.Column.FieldName))
                    return;

                var view = sender as GridView;
                var idObj = view.GetRowCellValue(e.RowHandle, "SystemInfoID");
                if (idObj == null) return;
                int systemInfoId = Convert.ToInt32(idObj);

                var newValue = e.Value?.ToString();

                var system = allSystems?.FirstOrDefault(s => s.SystemInfoID == systemInfoId);
                if (system == null) return;

                var active = system.pcCodeInfo?.FirstOrDefault(p => p.ExpireDate == null);
                string prevVal = view.GetRowCellValue(e.RowHandle, e.Column)?.ToString();

                bool ok = true;
                string errorMessage = null;

                // ========================
                // ۱. اعتبارسنجی مقدار جدید
                // ========================
                if (string.IsNullOrWhiteSpace(newValue))
                {
                    ok = false;
                    errorMessage = $"{columnDisplayNames[e.Column.FieldName]} نمی‌تواند خالی باشد!";
                }
                else if (e.Column.FieldName == "PersonnelCode" && !int.TryParse(newValue, out _))
                {
                    ok = false;
                    errorMessage = "کد پرسنلی باید عددی باشد!";
                }

                // ========================
                // ۲. بررسی رکورد فعال
                // ========================
                if (ok && active == null)
                {
                    ok = false;
                    errorMessage = "برای این سیستم رکورد فعال (PcCode) یافت نشد.";
                }

                if (!ok)
                {
                    // بازگرداندن مقدار قبلی
                    MessageBox.Show(errorMessage ?? "مقدار وارد شده نامعتبر است.", "خطا در ورود اطلاعات", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    suppressCellValueChanged = true;
                    try
                    {
                        view.SetRowCellValue(e.RowHandle, e.Column, prevVal ?? "-");
                    }
                    finally
                    {
                        suppressCellValueChanged = false;
                    }
                    return;
                }

                // ========================
                // ۳. اعمال مقدار جدید به شیء active
                // ========================
                if (active != null)
                {
                    ApplyValueToActive(active, e.Column.FieldName, newValue);
                }

                // ========================
                // ۴. ذخیره و بروزرسانی
                // ========================
                if (SetValue(systemInfoId, active))
                {
                    MessageBox.Show(
                        $"تغییر در ستون {e.Column.FieldName} ذخیره شد.\nمقدار جدید: {newValue}",
                        "موفق",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("خطا در ذخیره تغییرات در پایگاه داده.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Cursor.Current = Cursors.WaitCursor;
                initGridControl();
                Cursor.Current = Cursors.Default;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در اعمال تغییر: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }


        private void ApplyValueToActive(dynamic active, string fieldName, string newValue)
        {
            switch (fieldName)
            {
                case "PcCode":
                    if (!string.Equals(active.PcCode, newValue, StringComparison.Ordinal))
                        active.PcCode = newValue;
                    break;

                case "UserFullName":
                    if (!string.Equals(active.UserFullName, newValue, StringComparison.Ordinal))
                        active.UserFullName = newValue;
                    break;

                case "PersonnelCode":
                    if (int.TryParse(newValue, out int parsedCode) && active.PersonnelCode != parsedCode)
                        active.PersonnelCode = parsedCode;
                    break;

                default:
                    // سایر ستون‌های Desc و غیره:
                    var prop = active.GetType().GetProperty(fieldName);
                    if (prop != null && prop.CanWrite)
                    {
                        var current = prop.GetValue(active)?.ToString();
                        if (!string.Equals(current, newValue, StringComparison.Ordinal))
                            prop.SetValue(active, newValue);
                    }
                    break;
            }
        }


        private bool SetValue(int systemInfoRef, PcCodeInfo NewPcCodeInfo)
        {
            DataInsertUpdateHelper helper = new DataInsertUpdateHelper();
            return helper.ExpireAndInsertPcCodeInfo(systemInfoRef, NewPcCodeInfo);
        }

        private void barButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            DataSet ds = new DataSet();


            var helper = new DataSelectHelperNoFilter();
            allSystems = helper.SelectAllFullSystemInfo();

            var transformedSystems = allSystems
                .Select((s, index) => new
                {
                    RowNumber = index + 1,   // شماره ردیف خودکار (شروع از 1)
                    SystemInfoID = s.SystemInfoID,
                    PcCode = GetSafeDesc(s.pcCodeInfo, x => x.PcCode),
                    IpAddress = s.NetworkAdapterInfo != null
                                ? s.NetworkAdapterInfo
                                    .Where(a => !string.IsNullOrWhiteSpace(a.IpAddress))
                                    .OrderByDescending(a => a.IsLAN)
                                    .ThenByDescending(a => a.IsEnabled)
                                    .Select(a => a.IpAddress.Trim())
                                    .FirstOrDefault()
                                : null,
                    UserFullName = GetSafeDesc(s.pcCodeInfo, x => x.UserFullName),
                    PersonnelCode = GetSafeDesc(s.pcCodeInfo, x => x.PersonnelCode.ToString()),
                    Unit = GetSafeDesc(s.pcCodeInfo, x => x.Unit),
                    Desc1 = GetSafeDesc(s.pcCodeInfo, x => x.Desc1),
                    Desc2 = GetSafeDesc(s.pcCodeInfo, x => x.Desc2),
                    Desc3 = GetSafeDesc(s.pcCodeInfo, x => x.Desc3),
                    Desc4 = GetSafeDesc(s.pcCodeInfo, x => x.Desc4),
                    Desc5 = GetSafeDesc(s.pcCodeInfo, x => x.Desc5),
                    Desc6 = GetSafeDesc(s.pcCodeInfo, x => x.Desc6),
                    Desc7 = GetSafeDesc(s.pcCodeInfo, x => x.Desc7),
                    InsertDate = s.InsertDate,
                    ExpireDate = s.ExpireDate != null ? s.ExpireDate : (DateTime?)null,

                    pcCodeInfo = s.pcCodeInfo ?? new List<PcCodeInfo>(),
                    RamSummaryInfo = s.RamSummaryInfo != null ? new List<RamSummaryInfo> { s.RamSummaryInfo } : new List<RamSummaryInfo>(),
                    RamModuleInfo = s.RamModuleInfo ?? new List<RamModuleInfo>(),
                    cpuInfo = s.cpuInfo != null ? new List<CpuInfo> { s.cpuInfo } : new List<CpuInfo>(),
                    gpuInfo = s.gpuInfo != null ? new List<GpuInfo> { s.gpuInfo } : new List<GpuInfo>(),
                    DiskInfo = s.DiskInfo ?? new List<DiskInfo>(),
                    NetworkAdapterInfo = s.NetworkAdapterInfo ?? new List<NetworkAdapterInfo>(),
                    monitorInfo = s.monitorInfo ?? new List<MonitorInfo>(),
                    motherboardInfo = s.motherboardInfo != null ? new List<MotherboardInfo> { s.motherboardInfo } : new List<MotherboardInfo>(),
                    systemEnvironmentInfo = s.systemEnvironmentInfo != null ? new List<SystemEnvironmentInfo> { s.systemEnvironmentInfo } : new List<SystemEnvironmentInfo>(),
                    OpticalDriveInfo = s.OpticalDriveInfo ?? new List<OpticalDriveInfo>(),
                })
                .ToList();



            var workbook = new XLWorkbook();

            // 1. شیت اصلی SystemInfo
            var dtSystemInfo = ds.Tables["SystemInfo"];
            var wsMain = workbook.Worksheets.Add("SystemInfo");
            wsMain.Cell(1, 1).InsertTable(dtSystemInfo, "SystemInfo", true);

            // 2. شیت جزئی‌ها
            // CpuInfo
            if (transformedSystems.SelectMany(s => s.cpuInfo).Any())
            {
                var cpuList = transformedSystems
                    .SelectMany(s => s.cpuInfo, (s, cpu) => new
                    {
                        s.SystemInfoID,
                        cpu.CpuInfoID,
                        cpu.Name,
                        cpu.Manufacturer,
                        cpu.InsertDate,
                        cpu.ExpireDate
                    }).ToList();

                var wsCpu = workbook.Worksheets.Add("CpuInfo");
                wsCpu.Cell(1, 1).InsertTable(cpuList, "CpuInfo", true);
            }

            // NetworkAdapterInfo
            if (transformedSystems.SelectMany(s => s.NetworkAdapterInfo).Any())
            {
                var netList = transformedSystems
                    .SelectMany(s => s.NetworkAdapterInfo, (s, net) => new
                    {
                        s.SystemInfoID,
                        net.NetworkAdapterInfoID,
                        net.Name,
                        net.MACAddress,
                        net.IpAddress,
                        net.IsEnabled,
                        net.IsLAN,
                        net.InsertDate,
                        net.ExpireDate
                    }).ToList();

                var wsNet = workbook.Worksheets.Add("NetworkAdapterInfo");
                wsNet.Cell(1, 1).InsertTable(netList, "NetworkAdapterInfo", true);
            }

            // می‌توانید بقیه جزئی‌ها مثل RamModuleInfo، GpuInfo، MonitorInfo و ... را هم به همین شکل اضافه کنید

            // ذخیره فایل
            workbook.SaveAs("SystemInfo_MasterDetail.xlsx");
            Cursor.Current = Cursors.Default;
        }

        private void btnSendMsg_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            FrmSendMsg frmSendMsg = new FrmSendMsg();
            frmSendMsg.ShowDialog();
        }
        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable table = new DataTable(typeof(T).Name);

            // پراپرتی‌ها شامل BaseClass هم باشند
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            // ساخت ستون‌ها
            foreach (var prop in props)
            {
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                table.Columns.Add(prop.Name, propType);
            }

            // پر کردن داده‌ها
            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    var val = props[i].GetValue(item);
                    values[i] = val ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        private void BtnVNC_ButtonClick(int rowHandle)
        {
            if (rowHandle < 0) return;

            string IpAddress = gridView1.GetRowCellValue(rowHandle, "IpAddress")?.ToString();
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                MessageBox.Show("IP معتبر نیست.", "هشدار", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string vncPath = @"C:\Program Files\RealVNC\VNC Viewer\vncviewer.exe"; // مسیر VNC
            if (!File.Exists(vncPath))
            {
                MessageBox.Show("مسیر VNC یافت نشد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Process.Start(vncPath, IpAddress);
        }

        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            GridView view = sender as GridView;
            Point pt = view.GridControl.PointToClient(Control.MousePosition);
            GridHitInfo hitInfo = view.CalcHitInfo(pt);

            if (hitInfo.InRowCell)
            {
                int rowHandle = hitInfo.RowHandle;
                view.FocusedRowHandle = rowHandle;
                view.MakeRowVisible(rowHandle);
                // مقدار ستون VNC را بخوان
                bool isVncInstalled = Convert.ToBoolean(view.GetRowCellValue(rowHandle, "VNC"));

                if (!isVncInstalled)
                {
                    MessageBox.Show("مسیر VNC یافت نشد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                view.FocusedRowHandle = hitInfo.RowHandle;
                view.MakeRowVisible(hitInfo.RowHandle);

                string targetColumnFieldName = "VNCConnect"; // نام ستون مورد نظر
                if (hitInfo.Column != null && hitInfo.Column.FieldName == targetColumnFieldName)
                {
                    BtnVNC_ButtonClick(hitInfo.RowHandle);
                }
                else
                {
                    view.FocusedRowHandle = hitInfo.RowHandle;
                }
            }
        }

        private void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            loadGrid();
        }
    }
}