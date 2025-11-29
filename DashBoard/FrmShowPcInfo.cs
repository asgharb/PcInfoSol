using DashBoard.Data;
using DashBoard.Extention;
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class FrmShowPcInfo : Form
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
        public FrmShowPcInfo()
        {
            InitializeComponent();
        }

        private async void FrmShowPcInfo_Load(object sender, EventArgs e)
        {
            // هیچ چیز روی فرم تنظیم نمی‌شود تا داده‌ها آماده شوند.
            await InitializeDataAsync();

            Thread.Sleep(1000);
        }

        private async Task InitializeDataAsync()
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

            try
            {
                var helper = new DataSelectHelperNoFilter();
                allSystems = helper.SelectAllFullSystemInfo();

                var macs = allSystems.Select(s => s.NetworkAdapterInfo?
                                 .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.MACAddress))?.MACAddress)
                                 .Where(mac => !string.IsNullOrWhiteSpace(mac))
                                 .ToList();


                var transformedSystems = allSystems
                    .Select((s, index) => new
                    {
                        //No = index + 1,
                        SystemInfoID = s.SystemInfoID,
                        PcCode = int.Parse(GetSafeDesc(s.pcCodeInfo, x => x.PcCode).ToString()),
                        //IpAddress = s.NetworkAdapterInfo?
                        //           .Where(a =>
                        //               a.ExpireDate == null &&
                        //               !string.IsNullOrWhiteSpace(a.IpAddress))
                        //           .OrderByDescending(a => a.IsLAN)
                        //           .ThenByDescending(a => a.IsEnabled)
                        //           .Select(a => a.IpAddress.Trim())
                        //           .FirstOrDefault(),

                        //MacAddress = s.NetworkAdapterInfo?
                        //           .Where(a =>
                        //               a.ExpireDate == null &&
                        //               !string.IsNullOrWhiteSpace(a.MACAddress))
                        //           .OrderByDescending(a => a.IsLAN)
                        //           .ThenByDescending(a => a.IsEnabled)
                        //           .Select(a => a.MACAddress.Trim())
                        //           .FirstOrDefault(),

                        IpAddress= GetSafesSwitchInfo(s.SwithInfo, x => x.PcIp),
                        MacAddress = GetSafesSwitchInfo(s.SwithInfo, x => x.PcMac),
                        Switch_IP = GetSafesSwitchInfo(s.SwithInfo, x => x.SwitchIp),
                        Switch_Port = GetSafesSwitchInfo(s.SwithInfo, x => x.SwitchPort),
                        PhoneMac = GetSafesSwitchInfo(s.SwithInfo, x => x.PhoneMac),
                        PhoneIp = GetSafesSwitchInfo(s.SwithInfo, x => x.PhoneIp),
                        UserFullName = GetSafeDesc(s.pcCodeInfo, x => x.UserFullName),
                        PersonnelCode = GetSafeDesc(s.pcCodeInfo, x => x.PersonnelCode.ToString()),
                        Unit = GetSafeDesc(s.pcCodeInfo, x => x.Unit),
                        Desc1 = GetSafeDesc(s.pcCodeInfo, x => x.Desc1),
                        Desc2 = GetSafeDesc(s.pcCodeInfo, x => x.Desc2),
                        Desc3 = GetSafeDesc(s.pcCodeInfo, x => x.Desc3),
                        Desc4 = GetSafeDesc(s.pcCodeInfo, x => x.Desc4),
                        //Desc5 = GetSafeDesc(s.pcCodeInfo, x => x.Desc5),
                        //Desc6 = GetSafeDesc(s.pcCodeInfo, x => x.Desc6),
                        //Desc7 = GetSafeDesc(s.pcCodeInfo, x => x.Desc7),
                        VNC = GetSafeEnvironmentInfo(s.systemEnvironmentInfo, x => x.IsRealVNCInstalled, false),
                        Semantic = GetSafeEnvironmentInfo(s.systemEnvironmentInfo, x => x.IsSemanticInstalled, false),
                        AppVersion = GetSafeEnvironmentInfo(s.systemEnvironmentInfo, x => x.AppVersion, "0.0.0.0"),
                        pcCodeInfo = Utils.ToDtoList(s.pcCodeInfo),
                        systemEnvironmentInfo = Utils.ToDtoList(s.systemEnvironmentInfo),
                        RamSummaryInfo = Utils.ToDtoListSingle(s.RamSummaryInfo),
                        RamModuleInfo = Utils.ToDtoList(s.RamModuleInfo),
                        cpuInfo = Utils.ToDtoListSingle(s.cpuInfo),
                        gpuInfo = Utils.ToDtoListSingle(s.gpuInfo),
                        DiskInfo = Utils.ToDtoList(s.DiskInfo),
                        NetworkAdapterInfo = Utils.ToDtoList(s.NetworkAdapterInfo),
                        monitorInfo = Utils.ToDtoList(s.monitorInfo),
                        motherboardInfo = Utils.ToDtoListSingle(s.motherboardInfo),
                        OpticalDriveInfo = Utils.ToDtoList(s.OpticalDriveInfo)
                    })
                     .OrderBy(sys => sys.PcCode)
                     .ToList();


                var dtSystemInfo = ToDataTable(transformedSystems);
                dtSystemInfo.TableName = "SystemInfo";

                var ds = new DataSet();
                ds.Tables.Add(dtSystemInfo);

                InitGridAndBind(ds);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در بارگذاری داده‌ها: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void InitGridAndBind(DataSet ds)
        {
            gridControl1.DataSource = ds;
            gridControl1.DataMember = "SystemInfo";
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

            gridView1.Columns["SystemInfoID"].Visible = false;
            gridView1.RowHeight = 35;

            // ساخت ستون VNC بعد از دریافت داده‌ها
            if (gridView1.Columns["VNCConnect"] == null)
            {
                var btnVNC = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
                btnVNC.Buttons[0].Caption = "اتصال";
                btnVNC.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                btnVNC.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;

                Image resized = new Bitmap(Properties.Resources.vnc_3, new Size(48 * 3, 32));
                btnVNC.Buttons[0].ImageOptions.Image = resized;

                var colVNC = gridView1.Columns.AddVisible("VNCConnect", "اتصال VNC");
                colVNC.ColumnEdit = btnVNC;
                colVNC.Width = 100;
                gridControl1.RepositoryItems.Add(btnVNC);
                colVNC.OptionsColumn.AllowEdit = true;
                gridView1.OptionsBehavior.Editable = true;
            }

            gridView1.RowStyle -= gridView1_RowStyle;
            gridView1.RowStyle += gridView1_RowStyle;

            //gridView1.RowCellClick += GridView1_RowCellClick;
            gridView1.DoubleClick += gridView1_DoubleClick;

            SetupGridForPcCodeEditing();
        }

        private void GridView1_RowCellClick(object sender, RowCellClickEventArgs e)
        {
            // فقط در صورت دابل‌کلیک ادامه بده
            //if (e.Clicks == 2 && e.RowHandle >= 0)
            //{
            //    string field = e.Column.FieldName;

            //    switch (field)
            //    {
            //        case "VNCConnect":
            //            bool isVncInstalled = Convert.ToBoolean(gridView1.GetRowCellValue(e.RowHandle, "VNC"));
            //            if (!isVncInstalled)
            //            {
            //                MessageBox.Show("نرم‌افزار VNC نصب نشده یا مسیر یافت نشد.",
            //                                "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //                return;
            //            }
            //            BtnVNC_ButtonClick(e.RowHandle);
            //            break;
            //    }
            //}
        }


        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            GridView view = sender as GridView;
            Point pt = view.GridControl.PointToClient(Control.MousePosition);
            GridHitInfo hitInfo = view.CalcHitInfo(pt);

            if (!hitInfo.InRowCell) return;

            int rowHandle = hitInfo.RowHandle;
            view.FocusedRowHandle = rowHandle;
            view.MakeRowVisible(rowHandle);

            // فقط اگر روی ستون "VNCConnect" کلیک شده بود
            string targetColumnFieldName = "VNCConnect";
            if (hitInfo.Column != null && hitInfo.Column.FieldName == targetColumnFieldName)
            {
                // بررسی نصب بودن VNC فقط در این حالت
                bool isVncInstalled = Convert.ToBoolean(view.GetRowCellValue(rowHandle, "VNC"));
                if (!isVncInstalled)
                {
                    MessageBox.Show("مسیر VNC یافت نشد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // اجرای تابع اصلی
                BtnVNC_ButtonClick(rowHandle);
            }
        }



        void gridView1_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.LightGray : System.Drawing.Color.WhiteSmoke;
        }

        private string GetSafeDesc(IList<PcCodeInfo> list, Func<PcCodeInfo, string> selector)
        {
            if (list == null || list.Count == 0)
                return "-";

            // فقط آیتم‌هایی که ExpireDate == null دارند در نظر گرفته شوند
            var validItems = list.Where(x => x.ExpireDate == null).ToList();
            if (validItems.Count == 0)
                return "-";

            var value = selector(validItems.Last());
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }


        private T GetSafeEnvironmentInfo<T>(IList<SystemEnvironmentInfo> list, Func<SystemEnvironmentInfo, T> selector, T defaultValue = default)
        {
            if (list == null || list.Count == 0)
                return defaultValue;

            var lastItem = list.Where(x => x.ExpireDate == null).LastOrDefault();
            if (lastItem == null)
                return defaultValue;

            var value = selector(lastItem);

            if (value == null)
                return defaultValue;

            return value;
        }

        private string GetSafesSwitchInfo(SwithInfo Info, Func<SwithInfo, string> selector)
        {
            if (Info == null)
                return "-";

            var value = selector(Info);
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
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
                c.AppearanceCell.BackColor = System.Drawing.Color.White;
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

        private async void MasterView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
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

                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                await Task.Delay(1000);
                await InitializeDataAsync();
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;

            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در اعمال تغییر: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
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
        string NormalizeMac(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var hex = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
            return hex.Length == 12 ? hex.ToLower() : null;
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await Task.Delay(1000);
            await InitializeDataAsync();
        }

        private async void btnRefreshInfo_Click(object sender, EventArgs e)
        {
            await Task.Delay(1000);
            await InitializeDataAsync();
        }

        private void btnRefreshMac_Click(object sender, EventArgs e)
        {
            SwichIpRange swichIpRange = new SwichIpRange();
            swichIpRange.ShowDialog();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            btnRefreshInfo_Click(sender, e);
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }
    }
}
