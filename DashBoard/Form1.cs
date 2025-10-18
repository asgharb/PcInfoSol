using DashBoard.Data;
using DashBoard.Entity.Main;
using DashBoard.Entity.Models;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loadGrid();

            gridControl1.UseEmbeddedNavigator = true;

            SetupGridForPcCodeEditing();

        }

        private void loadGrid()
        {


            gridControl1.DataSource = null;
            gridControl1.DataMember = null;

            DataSet ds = new DataSet();

            var helper = new DataSelectHelper();
            List<SystemInfo> allSystems = helper.SelectAllSystemInfoWithRelations();


            // ایجاد یک کلاس داینامیک یا anonymous که همه فیلدهای تک را هم به لیست تبدیل کند
            var transformedSystems = allSystems
                .Select((s, index) => new
                {
                    RowNumber = index + 1,   // شماره ردیف خودکار (شروع از 1)
                    SystemInfoID = s.SystemInfoID,
                    pcCode = (s.pcCodeInfo != null && s.pcCodeInfo.Count > 0)
    ? (s.pcCodeInfo[s.pcCodeInfo.Count - 1].PcCodeName ?? "-")
    : "-",

            InsertDate = s.InsertDate,
                    ExpireDate = s.ExpireDate,

                    // همه فیلدهای تکی را داخل یک لیست می‌گذاریم
                    systemEnvironmentInfo = s.systemEnvironmentInfo != null ? new List<SystemEnvironmentInfo> { s.systemEnvironmentInfo } : new List<SystemEnvironmentInfo>(),
                    //pcCodeInfo = s.pcCodeInfo != null ? new List<PcCodeInfo> { s.pcCodeInfo } : new List<PcCodeInfo>(),
                    cpuInfo = s.cpuInfo != null ? new List<CpuInfo> { s.cpuInfo } : new List<CpuInfo>(),
                    gpuInfo = s.gpuInfo != null ? new List<GpuInfo> { s.gpuInfo } : new List<GpuInfo>(),
                    motherboardInfo = s.motherboardInfo != null ? new List<MotherboardInfo> { s.motherboardInfo } : new List<MotherboardInfo>(),
                    RamSummaryInfo = s.RamSummaryInfo != null ? new List<RamSummaryInfo> { s.RamSummaryInfo } : new List<RamSummaryInfo>(),

                    // فیلدهای لیست که از قبل لیست هستند را مستقیم قرار می‌دهیم
                    pcCodeInfo=s.pcCodeInfo?? new List<PcCodeInfo>(),
                    DiskInfo = s.DiskInfo ?? new List<DiskInfo>(),
                    NetworkAdapterInfo = s.NetworkAdapterInfo ?? new List<NetworkAdapterInfo>(),
                    RamModuleInfo = s.RamModuleInfo ?? new List<RamModuleInfo>(),
                    OpticalDriveInfo = s.OpticalDriveInfo ?? new List<OpticalDriveInfo>(),
                    monitorInfo = s.monitorInfo ?? new List<MonitorInfo>()
                })
                .ToList();



            DataTable dtSystemInfo = ToDataTable(transformedSystems);
            dtSystemInfo.TableName = "SystemInfo";
            dtSystemInfo.Columns["pcCode"].ReadOnly = false;
            ds.Tables.Add(dtSystemInfo);


            gridControl1.DataSource = ds;
            gridControl1.DataMember = "SystemInfo";
        }
        private GridView masterView;

        private void SetupGridForPcCodeEditing()
        {
            // 1) Force initialize so views/columns are created
            gridControl1.ForceInitialize();

            // 2) identify master view
            masterView = gridControl1.MainView as GridView;
            if (masterView == null) return;

            // 3) Make master view editable in-place
            masterView.OptionsBehavior.Editable = true;
            masterView.OptionsBehavior.EditingMode = GridEditingMode.Inplace;

            // 4) Ensure master datacolumn is writable (you already set this, but keep it safe)
            var dt = ((DataSet)gridControl1.DataSource).Tables["SystemInfo"];
            if (dt != null && dt.Columns.Contains("pcCode"))
                dt.Columns["pcCode"].ReadOnly = false;

            // 5) Configure master view: lock all except pcCode
            foreach (GridColumn col in masterView.Columns)
            {
                col.OptionsColumn.AllowEdit = false;
                col.AppearanceCell.BackColor = Color.White;
            }
            if (masterView.Columns["pcCode"] != null)
            {
                masterView.Columns["pcCode"].OptionsColumn.AllowEdit = true;
                //masterView.Columns["pcCode"].AppearanceCell.BackColor = Color.LightYellow;
            }

            // 6) Ensure detail views (existing ones) are non-editable
            foreach (BaseView baseView in gridControl1.Views)
            {
                if (baseView is GridView gv && gv != masterView)
                {
                    DisableEditingOnView(gv);
                }
            }

            // 7) Listen for detail views that get registered later (on-demand)
            gridControl1.ViewRegistered -= GridControl1_ViewRegistered;
            gridControl1.ViewRegistered += GridControl1_ViewRegistered;

            // 8) Ensure only pcCode opens editor in master
            masterView.ShowingEditor -= MasterView_ShowingEditor;
            masterView.ShowingEditor += MasterView_ShowingEditor;

            // 9) Handle value changed on master only
            masterView.CellValueChanged -= MasterView_CellValueChanged;
            masterView.CellValueChanged += MasterView_CellValueChanged;
        }

        private void DisableEditingOnView(GridView gv)
        {
            // make entire view read-only (so AllowEdit on columns is ignored)
            gv.OptionsBehavior.Editable = false;

            // also explicitly set column AllowEdit = false to be safe
            foreach (GridColumn c in gv.Columns)
            {
                c.OptionsColumn.AllowEdit = false;
                c.AppearanceCell.BackColor = Color.White;
            }
            // if a detail unexpectedly has a pcCode column, keep it non-editable
            if (gv.Columns["pcCode"] != null)
                gv.Columns["pcCode"].OptionsColumn.AllowEdit = false;
        }

        // This event fires when GridControl registers a new view (Detail views are often registered dynamically)
        private void GridControl1_ViewRegistered(object sender, ViewOperationEventArgs e)
        {
            if (e.View is GridView gv)
            {
                // if it's not the master, disable editing on it
                if (gv != masterView)
                    DisableEditingOnView(gv);
            }
        }

        // Only allow editor for pcCode on master
        private void MasterView_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var view = sender as GridView;
            if (view == null) return;

            if (view.FocusedColumn == null || view.FocusedColumn.FieldName != "pcCode")
                e.Cancel = true;
        }

        // react to changes in master pcCode
        private void MasterView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName != "pcCode") return;

            var view = sender as GridView;
            var idObj = view.GetRowCellValue(e.RowHandle, "SystemInfoID");
            int id = idObj != null ? Convert.ToInt32(idObj) : -1;
            string newPc = e.Value?.ToString();

            UpdatePcCodeInDatabase(id, newPc);
        }


        private void UpdatePcCodeInDatabase(int id, string newPcCode)
        {
            if (newPcCode == null) return;

            DataSelectHelper dataSelectHelper = new DataSelectHelper();
            DataUpdateHelper dataUpdateHelper = new DataUpdateHelper();

            SystemInfo system = dataSelectHelper.SelectByPrimaryKey<SystemInfo>(id); // شی SystemInfo از دیتابیس

            // بروز رسانی PcCodeName به صورت Generic
            dataUpdateHelper.UpdateChildWithHistory<PcCodeInfo>(id, "PcCodeName", newPcCode);
            MessageBox.Show($"ok");

        }


        //public static DataTable ToDataTable<T>(List<T> items)
        //{
        //    DataTable table = new DataTable(typeof(T).Name);
        //    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    foreach (var prop in props)
        //    {
        //        table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        //    }

        //    foreach (var item in items)
        //    {
        //        var values = new object[props.Length];
        //        for (int i = 0; i < props.Length; i++)
        //            values[i] = props[i].GetValue(item) ?? DBNull.Value;
        //        table.Rows.Add(values);
        //    }

        //    return table;
        //}


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


    }
}
