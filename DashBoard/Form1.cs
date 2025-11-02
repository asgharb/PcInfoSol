using ClosedXML.Excel;
using DashBoard.Data;
using DashBoard.Extention;
using DevExpress.LookAndFeel;
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
        // حافظه محلی داده‌ها
        List<SystemInfo> allSystems;

        // لیست ستون‌های قابل ویرایش (قابل تغییر در زمان اجرا با SetEditableColumns)
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

            //gridView1.Appearance.FocusedRow.BackColor = System.Drawing.Color.Green;
            //gridView1.Appearance.FocusedRow.ForeColor = System.Drawing.Color.White;

            ////// پس از InitializeComponent یا در Load فرم:
            ////gridView1.Appearance.Row.BackColor = Color.White;        // پس‌زمینه ردیف‌ها
            ////gridView1.Appearance.Row.ForeColor = Color.Black;        // رنگ نوشته در ردیف‌ها
            ////gridView1.Appearance.OddRow.BackColor = Color.FromArgb(30, 30, 30);   // برای ردیف‌های فرد اگر خواستید متمایز
            ////gridView1.OptionsView.EnableAppearanceOddRow = true;     // فعال کردن
            ////gridView1.Appearance.FocusedRow.BackColor = Color.Green; // رنگ پس‌زمینه ردیف انتخاب شده



            //var view = gridView1;

            //gridView1.Appearance.Row.BackColor = Color.Black;        // پس‌زمینه سطرها
            //gridView1.Appearance.Row.ForeColor = Color.White;        // متن‌ها سفید
            //gridView1.Appearance.Row.Options.UseBackColor = true;
            //gridView1.Appearance.Row.Options.UseForeColor = true;

            //// اگر می‌خوای ردیف انتخاب شده سبز شود
            //gridView1.Appearance.FocusedRow.BackColor = Color.Green;
            //gridView1.Appearance.FocusedRow.ForeColor = Color.White;
            //gridView1.Appearance.FocusedRow.Options.UseBackColor = true;
            //gridView1.Appearance.FocusedRow.Options.UseForeColor = true;

            //// برای حالت Flat و جلوگیری از تداخل اسکین
            //gridView1.PaintStyleName = "Flat";

            //// اگر می‌خواهی ردیف انتخاب‌شده سبز شود
            //gridView1.Appearance.FocusedRow.BackColor = Color.Green;
            //gridView1.Appearance.FocusedRow.ForeColor = Color.White;
            //gridView1.Appearance.FocusedRow.Options.UseBackColor = true;
            //gridView1.Appearance.FocusedRow.Options.UseForeColor = true;

            // اختیاری: اگر Grid از تم خاصی استفاده می‌کند
            //gridView1.PaintStyleName = "Flat";  // تم ساده‌تر برای دید واضح‌تر رنگ‌ها



            //// 🚫 غیرفعال‌کردن رنگ‌های اسکین برای تمام حالت‌ها
            ////view.Appearance.Row.Options.UseDefaultBackColor = false;
            ////view.Appearance.Row.Options.UseDefaultForeColor = false;

            // 🌑 رنگ ردیف‌های معمولی
            //view.Appearance.Row.BackColor = Color.Black;
            //view.Appearance.Row.ForeColor = Color.Black;
            //view.Appearance.Row.Options.UseBackColor = true;
            //view.Appearance.Row.Options.UseForeColor = true;

            //// 🌚 برای تمایز بین ردیف‌های فرد و زوج
            //view.OptionsView.EnableAppearanceOddRow = false;
            //view.OptionsView.EnableAppearanceEvenRow = false;

            //// 🌱 رنگ سطر انتخاب‌شده
            //view.Appearance.FocusedRow.BackColor = Color.Green;
            //view.Appearance.FocusedRow.ForeColor = Color.White;
            //view.Appearance.FocusedRow.Options.UseBackColor = true;
            //view.Appearance.FocusedRow.Options.UseForeColor = true;

            //view.Appearance.SelectedRow.BackColor = Color.Green;
            //view.Appearance.SelectedRow.ForeColor = Color.White;
            //view.Appearance.SelectedRow.Options.UseBackColor = true;
            //view.Appearance.SelectedRow.Options.UseForeColor = true;

            //// 🧱 رنگ فضای خالی گرید
            //view.Appearance.Empty.BackColor = Color.Black;
            //view.Appearance.Empty.Options.UseBackColor = true;

            //// 🔠 رنگ و فونت سرستون‌ها
            //view.Appearance.HeaderPanel.BackColor = Color.FromArgb(50, 50, 50);
            //view.Appearance.HeaderPanel.ForeColor = Color.White;
            //view.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            //view.Appearance.HeaderPanel.Options.UseBackColor = true;
            //view.Appearance.HeaderPanel.Options.UseForeColor = true;
            //view.Appearance.HeaderPanel.Options.UseFont = true;

            //// ⚙️ سایر تنظیمات ظاهری برای جلوگیری از تداخل اسکین
            //view.PaintStyleName = "Flat"; // حالت مسطح و بدون تم پوسته
            //view.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            //view.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.True;
            //// اگر می‌خواهی مرزهای سلول‌ها هم دیده نشن:
            //// view.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.False;
            //// view.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;


            ////(gridControl1.MainView as GridView).RowStyle;
            ///

            //gridControl1.LookAndFeel.UseDefaultLookAndFeel = false;
            //gridControl1.LookAndFeel.Style = LookAndFeelStyle.Flat;
            //// می‌توانی یک skin تیره هم ست کنی اگر خواستی:
            //// gridControl1.LookAndFeel.SkinName = "Office 2016 Black";

            //var view = gridView1 as GridView;

            //// 2) اطمینان از اینکه تنظیمات ظاهر ما اعمال می‌شوند:
            //// (استفاده از AppearanceOptions.UseBackColor / UseForeColor صحیح است؛
            ////  نامی به شکل UseDefaultBackColor وجود ندارد)
            //// سطرها (همه): پس‌زمینه مشکی و متن سفید
            //view.Appearance.Row.BackColor = Color.Black;
            //view.Appearance.Row.ForeColor = Color.White;
            //view.Appearance.Row.Options.UseBackColor = true;
            //view.Appearance.Row.Options.UseForeColor = true;

            //// فضای خالیِ گرید
            //view.Appearance.Empty.BackColor = Color.Black;
            //view.Appearance.Empty.Options.UseBackColor = true;

            //// هدر ستون‌ها
            //view.Appearance.HeaderPanel.BackColor = Color.FromArgb(40, 40, 40);
            //view.Appearance.HeaderPanel.ForeColor = Color.White;
            //view.Appearance.HeaderPanel.Options.UseBackColor = true;
            //view.Appearance.HeaderPanel.Options.UseForeColor = true;
            //view.Appearance.HeaderPanel.Options.UseFont = true;
            //view.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            //// 3) سطر انتخاب‌شده (Focused / Selected) => سبز
            //// حتما این گزینه‌ها را فعال کن تا این ظاهر در اولویت باشد:
            //view.Appearance.FocusedRow.BackColor = Color.FromArgb(0, 140, 0); // سبز
            //view.Appearance.FocusedRow.ForeColor = Color.White;
            //view.Appearance.FocusedRow.Options.UseBackColor = true;
            //view.Appearance.FocusedRow.Options.UseForeColor = true;

            //view.Appearance.SelectedRow.BackColor = Color.FromArgb(0, 140, 0);
            //view.Appearance.SelectedRow.ForeColor = Color.White;
            //view.Appearance.SelectedRow.Options.UseBackColor = true;
            //view.Appearance.SelectedRow.Options.UseForeColor = true;

            //// 4) بعضی از تنظیمات selection/focus ممکن است اولویت بالاتری داشته باشند.
            //// اگر رنگ سطرها باز هم توسط focused/selected بازنویسی شد، این گزینه‌ها را امتحان کن:
            //view.OptionsSelection.EnableAppearanceFocusedRow = true;  // اجازه میده focused row ظاهر خودش رو داشته باشه
            //view.OptionsSelection.EnableAppearanceHideSelection = false; // وقتی گرید focus رو از دست داد هم رنگ انتخاب حفظ شود (اختیاری)
            //view.OptionsSelection.MultiSelect = false; // یا true اگر می‌خواهی انتخاب چندتایی
            //view.PaintStyleName = "Flat"; // کمک می‌کند ظاهر بصورت ساده و بدون تصاویر اسکین کشیده شود

            //// 5) اگر از conditional formatting یا RowCellStyle استفاده می‌کنی،
            //// مطمئن شو که آن‌ها اولویت پایین‌تری ندارند یا مقادیر Options را نیز ست کرده‌ای.
            //// مثال: اگر مقداری سفید می‌آید از RowCellStyle استفاده کن تا اشکال رفع شود:
            //view.RowCellStyle += (s, ev) =>
            //{
            //    // فقط نمونه: برای اطمینان از فونت/رنگ تمام سلول‌ها
            //    ev.Appearance.ForeColor = Color.White;
            //    ev.Appearance.Options.UseForeColor = true;
            //    ev.Appearance.BackColor = Color.Black;
            //    ev.Appearance.Options.UseBackColor = true;
            //};
        }

        private void loadGrid()
        {
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
                    Image resized = new Bitmap(original, new Size(48*3, 32));

                    // اختصاص به دکمه
                    btnVNC.Buttons[0].ImageOptions.Image = resized;

                    gridControl1.RepositoryItems.Add(btnVNC);
                    colVNC.OptionsColumn.AllowEdit = true;
                    gridView1.OptionsBehavior.Editable = true;
                }


                gridView1.RowHeight = 35;
                gridView1.Columns["SystemInfoID"].Visible = false;

                gridView1.RowStyle += gridView1_RowStyle;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در بارگذاری داده‌ها: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    col.AppearanceCell.BackColor = Color.White;
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
        //private void MasterView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        //{
        //    Cursor.Current = Cursors.WaitCursor;
        //    if (suppressCellValueChanged) return; // اگر ما خودمون مقدار رو ست کرده باشیم، پردازش نکن

        //    try
        //    {
        //        if (!editableColumns.Contains(e.Column.FieldName)) return;

        //        var view = sender as GridView;
        //        var idObj = view.GetRowCellValue(e.RowHandle, "SystemInfoID");
        //        if (idObj == null) return;
        //        int systemInfoId = Convert.ToInt32(idObj);

        //        var newValue = e.Value?.ToString();

        //        // پیدا کردن system در حافظه
        //        var system = allSystems?.FirstOrDefault(s => s.SystemInfoID == systemInfoId);
        //        if (system == null) return;

        //        // گرفتن رکورد فعال و مقدار قبلی برای revert در صورت نیاز
        //        var active = system.pcCodeInfo?.FirstOrDefault(p => p.ExpireDate == null);
        //        string prevVal = null;
        //        if (active != null)
        //        {
        //            // گرفتن مقدار قبل بر اساس ستون
        //            switch (e.Column.FieldName)
        //            {
        //                case "PcCode": prevVal = active.PcCode; break;
        //                case "UserFullName": prevVal = active.UserFullName; break;
        //                case "PersonnelCode": prevVal = active.PersonnelCode.ToString(); break;
        //                default: prevVal = view.GetRowCellValue(e.RowHandle, e.Column)?.ToString(); break;
        //            }
        //        }
        //        else
        //        {
        //            // اگر رکورد فعال وجود ندارد، prevVal را از دیتاتیبل بگیر (projection)
        //            prevVal = view.GetRowCellValue(e.RowHandle, e.Column)?.ToString();
        //        }

        //        // اعتبارسنجی و اعمال تغییرات
        //        bool ok = true;
        //        string errorMessage = null;

        //        switch (e.Column.FieldName)
        //        {
        //            case "PcCode":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = "کد سیستم (PcCode) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.PcCode, newValue, StringComparison.Ordinal))
        //                                active.PcCode = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }

        //            case "UserFullName":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = "نام کاربر (UserFullName) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.UserFullName, newValue, StringComparison.Ordinal))
        //                                active.UserFullName = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }

        //            case "PersonnelCode":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = "کد پرسنلی (PersonnelCode) نمی‌تواند خالی باشد!";
        //                    }
        //                    else if (!int.TryParse(newValue, out int parsed))
        //                    {
        //                        ok = false;
        //                        errorMessage = "کد پرسنلی باید عددی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (active.PersonnelCode != parsed)
        //                                active.PersonnelCode = parsed;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }

        //            case "Desc1":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = " (Desc1) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.Desc1, newValue, StringComparison.Ordinal))
        //                                active.Desc1 = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }
        //            case "Desc2":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = " (Desc2) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.Desc2, newValue, StringComparison.Ordinal))
        //                                active.Desc2 = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }
        //            case "Desc3":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = " (Desc3) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.Desc3, newValue, StringComparison.Ordinal))
        //                                active.Desc3 = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }

        //            case "Desc4":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = " (Desc4) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.Desc4, newValue, StringComparison.Ordinal))
        //                                active.Desc4 = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }
        //            case "Desc5":
        //                {
        //                    if (string.IsNullOrWhiteSpace(newValue))
        //                    {
        //                        ok = false;
        //                        errorMessage = " (Desc5) نمی‌تواند خالی باشد!";
        //                    }
        //                    else
        //                    {
        //                        if (active != null)
        //                        {
        //                            if (!string.Equals(active.Desc5, newValue, StringComparison.Ordinal))
        //                                active.Desc5 = newValue;
        //                        }
        //                        else
        //                        {
        //                            ok = false;
        //                            errorMessage = "برای این سیستم رکورد PcCode فعالی یافت نشد.";
        //                        }
        //                    }
        //                    break;
        //                }

        //            default:
        //                {
        //                    // ستون‌های دیگر: اگر نیاز به منطق دارن اینجا اضافه کن
        //                    break;
        //                }
        //        }

        //        if (!ok)
        //        {
        //            // خطا: پیغام نشان بده و مقدار سلول را به مقدار قبلی برگردان
        //            MessageBox.Show(errorMessage ?? "مقدار وارد شده نامعتبر است.", "خطا در ورود اطلاعات", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        //            // جلوگیری از تریگر دوباره با SetRowCellValue
        //            suppressCellValueChanged = true;
        //            try
        //            {
        //                view.SetRowCellValue(e.RowHandle, e.Column, prevVal ?? "-");
        //            }
        //            finally
        //            {
        //                // تا وقتی که SetRowCellValue اجرا می‌شود، رویداد دوباره به handler می‌رسد اما نادیده گرفته خواهد شد
        //                suppressCellValueChanged = false;
        //            }
        //            return;
        //        }

        //        // اگر رسیدیم اینجا یعنی ok هست — میتوانی اینجا عملیات ذخیره در DB را صدا بزنی
        //        // مثال: SetValue(systemInfoId, active); // یا متد ExpireAndInsert در DataInsertUpdateHelper

        //        // بروزرسانی UI/حافظه
        //        if (SetValue(systemInfoId, active))
        //        {
        //            MessageBox.Show(
        //            $"Column: {e.Column.FieldName}\nNew Value: {newValue}",
        //            "ویرایش ذخیره شد",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Information);

        //        }
        //        else
        //        {
        //            MessageBox.Show(
        //            $"خطا در ذخیره تغییرات در پایگاه داده.",
        //            "خطا",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error);
        //        }
        //        initGridControl(); // اگر این متد رفرش گرید را انجام می‌دهد


        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("خطا در اعمال تغییر: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    Cursor.Current = Cursors.Default;
        //}

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

            //DataTable dtSystemInfo = ToDataTable(transformedSystems);
            //dtSystemInfo.TableName = "SystemInfo";
            //ds.Tables.Add(dtSystemInfo);



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

    }
}




//private void SetupGridForPcCodeEditing()
//{
//    // 1) Force initialize so views/columns are created
//    gridControl1.ForceInitialize();

//    // 2) identify master view
//    masterView = gridControl1.MainView as GridView;
//    if (masterView == null) return;

//    // 3) Make master view editable in-place
//    masterView.OptionsBehavior.Editable = true;
//    masterView.OptionsBehavior.EditingMode = GridEditingMode.Inplace;

//    // 4) Ensure master datacolumn is writable (you already set this, but keep it safe)
//    var dt = ((DataSet)gridControl1.DataSource).Tables["SystemInfo"];
//    if (dt != null && dt.Columns.Contains("PcCode"))
//        dt.Columns["PcCode"].ReadOnly = false;

//    // 5) Configure master view: lock all except PcCode
//    foreach (GridColumn col in masterView.Columns)
//    {
//        col.OptionsColumn.AllowEdit = false;
//        col.AppearanceCell.BackColor = Color.White;
//    }
//    if (masterView.Columns["PcCode"] != null)
//    {
//        masterView.Columns["PcCode"].OptionsColumn.AllowEdit = true;
//        //masterView.Columns["PcCode"].AppearanceCell.BackColor = Color.LightYellow;
//    }

//    // 6) Ensure detail views (existing ones) are non-editable
//    foreach (BaseView baseView in gridControl1.Views)
//    {
//        if (baseView is GridView gv && gv != masterView)
//        {
//            DisableEditingOnView(gv);
//        }
//    }

//    // 7) Listen for detail views that get registered later (on-demand)
//    gridControl1.ViewRegistered -= GridControl1_ViewRegistered;
//    gridControl1.ViewRegistered += GridControl1_ViewRegistered;

//    // 8) Ensure only pcCode opens editor in master
//    masterView.ShowingEditor -= MasterView_ShowingEditor;
//    masterView.ShowingEditor += MasterView_ShowingEditor;

//    // 9) Handle value changed on master only
//    masterView.CellValueChanged -= MasterView_CellValueChanged;
//    masterView.CellValueChanged += MasterView_CellValueChanged;
//}

//private void DisableEditingOnView(GridView gv)
//{
//    // make entire view read-only (so AllowEdit on columns is ignored)
//    gv.OptionsBehavior.Editable = false;

//    // also explicitly set column AllowEdit = false to be safe
//    foreach (GridColumn c in gv.Columns)
//    {
//        c.OptionsColumn.AllowEdit = false;
//        c.AppearanceCell.BackColor = Color.White;
//    }
//    // if a detail unexpectedly has a pcCode column, keep it non-editable
//    if (gv.Columns["PcCode"] != null)
//        gv.Columns["PcCode"].OptionsColumn.AllowEdit = false;
//}

//private void GridControl1_ViewRegistered(object sender, ViewOperationEventArgs e)
//{
//    if (e.View is GridView gv)
//    {
//        // if it's not the master, disable editing on it
//        if (gv != masterView)
//            DisableEditingOnView(gv);
//    }
//}

//private void MasterView_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
//{
//    var view = sender as GridView;
//    if (view == null || view.FocusedColumn == null) return;

//    if (!editableColumns.Contains(view.FocusedColumn.FieldName))
//        e.Cancel = true;
//}

//private void MasterView_CellValueChanged(object sender, CellValueChangedEventArgs e)
//{
//    if (e.Column.FieldName != "PcCode") return;

//    var view = sender as GridView;
//    var idObj = view.GetRowCellValue(e.RowHandle, "SystemInfoID");
//    int SystemInfoRef = idObj != null ? Convert.ToInt32(idObj) : -1;


//    PcCodeInfo prePcCodeInfo = GetActivePcCode(allSystems, SystemInfoRef);
//    if (prePcCodeInfo != null && prePcCodeInfo.PcCode == e.Value?.ToString())
//    {
//        // No change in PcCode, do nothing
//        return;
//    }

//    //PcCodeInfo pcCodeInfo = new PcCodeInfo();
//    //pcCodeInfo.SystemInfoRef = SystemInfoRef;
//    //pcCodeInfo.PcCode = e.Value?.ToString(); 
//    //pcCodeInfo.UserFullName = idObj;
//    //pcCodeInfo.SystemInfoRef = idObj;
//    //pcCodeInfo.SystemInfoRef = idObj;

//}





//systemEnvironmentInfo = s.systemEnvironmentInfo != null ? new List<SystemEnvironmentInfo> { s.systemEnvironmentInfo } : new List<SystemEnvironmentInfo>(),
//RamSummaryInfo = s.RamSummaryInfo != null ? new List<RamSummaryInfo> { s.RamSummaryInfo } : new List<RamSummaryInfo>(),
//RamModuleInfo = s.RamModuleInfo ?? new List<RamModuleInfo>(),
//cpuInfo = s.cpuInfo != null ? new List<CpuInfo> { s.cpuInfo } : new List<CpuInfo>(),
//gpuInfo = s.gpuInfo != null ? new List<GpuInfo> { s.gpuInfo } : new List<GpuInfo>(),
//DiskInfo = s.DiskInfo ?? new List<DiskInfo>(),
//NetworkAdapterInfo = s.NetworkAdapterInfo ?? new List<NetworkAdapterInfo>(),
//monitorInfo = s.monitorInfo ?? new List<MonitorInfo>(),
//motherboardInfo = s.motherboardInfo != null ? new List<MotherboardInfo> { s.motherboardInfo } : new List<MotherboardInfo>(),
//OpticalDriveInfo = s.OpticalDriveInfo ?? new List<OpticalDriveInfo>(),