using DashBoard.Data;
using DevExpress.XtraEditors;
using MyNetworkLib;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class FrmSwitchInfo : Form
    {
        public FrmSwitchInfo()
        {
            InitializeComponent();
        }

        private void FrmSwitchInfo_Load(object sender, EventArgs e)
        {
            InitializeDataAsync();
        }
        private async Task InitializeDataAsync()
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            try
            {
                var helper = new DataSelectHelper();
                List<SwithInfo> items = helper.SelectAll<SwithInfo>();

                var grouped = items
                    .GroupBy(x => x.SwitchIp)
                    .OrderBy(g => IpToUint(g.Key))
                    .Select(g => new SwitchGroup
                    {
                        SwitchIp = g.Key,
                        Records = g.Select(x => new SwithInfoLite
                        {
                            SwitchPort = x.SwitchPort,
                            PcMac = x.PcMac,
                            PcVlan = x.PcVlan,
                            PcIp = x.PcIp,
                            PhoneMac = x.PhoneMac,
                            PhoneVlan = x.PhoneVlan,
                            PhoneIp = x.PhoneIp
                        }).ToList()
                    })
                    .ToList();


                var dtSystemInfo = ToDataTable(grouped);
                dtSystemInfo.TableName = "SystemInfo";

                var ds = new DataSet();
                ds.Tables.Add(dtSystemInfo);

                gridControl1.DataSource = ds;
                gridControl1.DataMember = "SystemInfo";
                gridControl1.UseEmbeddedNavigator = true;
                ControlNavigator navigator = gridControl1.EmbeddedNavigator;

                gridView1.RowHeight = 35;
                gridView1.OptionsBehavior.Editable = false;
                gridView1.OptionsBehavior.ReadOnly = true;

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
        private static uint IpToUint(string ipString)
        {
            if (!IPAddress.TryParse(ipString, out var ip))
                return uint.MaxValue; // غیرقابل‌تبدیل -> آخر قرار گیرد

            var bytes = ip.GetAddressBytes(); // network order (big-endian)
            if (bytes.Length != 4)
                return uint.MaxValue; // اگر IPv6 یا نامعلوم -> آخر قرار گیرد

            // BitConverter.ToUInt32 انتظار little-endian دارد در سیستم‌های معمولی،
            // پس اگر سیستم LittleEndian است باید بایت‌ها را برگردانیم.
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        public class SwithInfoLite
        {
            public string SwitchPort { get; set; }
            public string PcMac { get; set; }
            public string PcVlan { get; set; }
            public string PcIp { get; set; }
            public string PhoneMac { get; set; }
            public string PhoneVlan { get; set; }
            public string PhoneIp { get; set; }
        }

        public class SwitchGroup
        {
            public string SwitchIp { get; set; }
            public List<SwithInfoLite> Records { get; set; }
        }


    }
}
