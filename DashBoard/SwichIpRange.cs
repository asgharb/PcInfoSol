using MyNetworkLib;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class SwichIpRange : Form
    {
        public static string startIp = "192.168.254.1";
        public static string endIp = "192.168.254.2";

        public static bool IsOkClicked = false;

        public SwichIpRange()
        {
            InitializeComponent();
        }


        private void SwichIpRange_Load(object sender, EventArgs e)
        {
            txtTo.Text = txtFrom.Text = "192.168.254.2";

            this.BackColor = Color.FromArgb(245, 245, 245);
            btnOk.FlatStyle= btnCancel.FlatStyle = FlatStyle.Flat;
            btnOk.BackColor= btnCancel.BackColor = Color.FromArgb(52, 152, 219);   // آبی 
            btnOk.ForeColor = btnCancel.ForeColor = Color.White;

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void txtFrom_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // جلوگیری از وارد کردن دو نقطه پشت سر هم
            TextBox txt = sender as TextBox;
            if (e.KeyChar == '.' && txt != null)
            {
                if (txt.Text.EndsWith("."))
                    e.Handled = true;
            }
        }

        private void txtTo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // جلوگیری از وارد کردن دو نقطه پشت سر هم
            TextBox txt = sender as TextBox;
            if (e.KeyChar == '.' && txt != null)
            {
                if (txt.Text.EndsWith("."))
                    e.Handled = true;
            }
        }



        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void btnOk_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            string from = txtFrom.Text;
            string to = txtTo.Text;

            txtFrom.Enabled = txtTo.Enabled = false;
            btnCancel.Enabled = btnOk.Enabled = false;

            Thread.Sleep(500); 
            if (!ValidateIpRange(from, to))
            {
                Cursor = Cursors.Default;
                txtFrom.Enabled = txtTo.Enabled = true;
                btnCancel.Enabled = btnOk.Enabled = true;
                return;
            }


            txtFrom.Enabled = txtTo.Enabled = false;
            btnCancel.Enabled = btnOk.Enabled = false;

            List<string> ips = NetworkMapper.GetIpRange(from, to);
            progressBar1.Minimum = 0;
            progressBar1.Maximum = ips.Count;
            progressBar1.Value = 0;

            var progress = new Progress<int>(val =>
            {
                progressBar1.Value = val;
            });

            // فراخوانی متد اسکن و دریافت نتیجه در متغیر results
            List<SwithInfo> results = await Task.Run(() =>
            {
                // توجه: متد ScanNetworkRange لیست را برمی‌گرداند
                return NetworkMapper.ScanNetworkRange(from, to, progress);
            });

            // حالا results پر شده است و می‌توانید آن را به دیتابیس بفرستید
            if (results != null && results.Count > 0)
            {
                var helper = new DataInsertUpdateHelper();

                // ارسال لیست به متد ذخیره‌سازی
                bool ok = helper.InsertSwithchinfos(results);

                Console.WriteLine(ok ? "درج انجام شد" : "خطا در درج یا لیست خالی بود");
            }
            else
            {
                Console.WriteLine("هیچ اطلاعاتی یافت نشد.");
            }

            MessageBox.Show("محدوده آی‌پی با موفقیت اسکن شد.");

            txtFrom.Enabled = txtTo.Enabled = true;
            btnCancel.Enabled = btnOk.Enabled = true;

            progressBar1.Value = 0;
            Cursor = Cursors.Default;
            IsOkClicked=true;
        }

        private bool ValidateIpRange(string ipFrom, string ipTo)
        {
            ipFrom = ipFrom.Trim();
            ipTo = ipTo.Trim();

            // 1. چک فرمت صحیح IP
            if (!IsValidIPv4(ipFrom))
            {
                MessageBox.Show("آی‌پی شروع معتبر نیست.");
                return false;
            }

            if (!IsValidIPv4(ipTo))
            {
                MessageBox.Show("آی‌پی پایان معتبر نیست.");
                return false;
            }

            // 2. تبدیل IP ها به عدد برای مقایسه
            long start = IpToLong(ipFrom);
            long end = IpToLong(ipTo);

            if (start > end)
            {
                MessageBox.Show("آی‌پی پایان نمی‌تواند کوچکتر از آی‌پی شروع باشد.");
                return false;
            }

            // 3. بررسی اینکه هر دو IP در یک Subnet هستند (اختیاری)
            // اگر نمی‌خواهی، این بخش را حذف کن
            var s1 = ipFrom.Split('.');
            var s2 = ipTo.Split('.');
            if (s1[0] != s2[0] || s1[1] != s2[1] || s1[2] != s2[2])
            {
                MessageBox.Show("آی‌پی‌ها باید در یک محدوده شبکه (Subnet) باشند.");
                return false;
            }

            return true;
        }
        private bool IsValidIPv4(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            var parts = ip.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int num))
                    return false;

                if (num < 0 || num > 255)
                    return false;
            }

            return true;
        }
        private long IpToLong(string ip)
        {
            var parts = ip.Split('.').Select(int.Parse).ToArray();
            return ((long)parts[0] << 24)
                 | ((long)parts[1] << 16)
                 | ((long)parts[2] << 8)
                 | parts[3];
        }
    }
}
