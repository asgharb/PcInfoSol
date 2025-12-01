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
                    .Select(g => new
                    {
                        SwitchIp = g.Key,
                        Details = g.OrderBy(x => ExtractPortNumber(x.SwitchPort))
                                 .Select(x => new
                                 {
                                     UserFullName = x.UserFullName,
                                     SwitchPort = x.SwitchPort,
                                     PcMac = x.PcMac,
                                     PcVlan = x.PcVlan,
                                     PcIp = x.PcIp,
                                     PhoneMac = x.PhoneMac,
                                     PhoneVlan = x.PhoneVlan,
                                     PhoneIp = x.PhoneIp,
                                     VTMac = x.VTMac,
                                     VTIp = x.VTIP,
                                     VTVlan = x.VTVlan
                                 }).ToList()
                    })
                    .ToList();



                var dtSwInfo = ToDataTable(grouped);
                dtSwInfo.TableName = "SwInfo";

                var dsSw = new DataSet();
                dsSw.Tables.Add(dtSwInfo);

                gridControl_1.DataSource = dsSw;
                gridControl_1.DataMember = "SwInfo";
                gridControl_1.UseEmbeddedNavigator = true;
                ControlNavigator navigator = gridControl_1.EmbeddedNavigator;

                gridView_1.RowHeight = 35;
                gridView_1.OptionsBehavior.Editable = false;
                gridView_1.OptionsBehavior.ReadOnly = true;

                gridView_1.RowStyle -= gridView1_RowStyle;
                gridView_1.RowStyle += gridView1_RowStyle;

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

        private int ExtractPortNumber(string port)
        {
            if (string.IsNullOrWhiteSpace(port))
                return -1;

            // آخرین بخش بعد از "/" را استخراج می‌کند
            var parts = port.Split('/');
            int num;

            if (int.TryParse(parts.Last(), out num))
                return num;

            return -1;
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

        void gridView1_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.LightGray : System.Drawing.Color.WhiteSmoke;
        }

    }
}

