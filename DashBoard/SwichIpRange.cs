using MyNetworkLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class SwichIpRange : Form
    {
        public static string startIp = "1720.20.254.1";
        public static string endIp = "1720.20.254.1";
        public SwichIpRange()
        {
            InitializeComponent();
        }


        private void SwichIpRange_Load(object sender, EventArgs e)
        {
            txtTo.Text = txtFrom.Text = "192.168.254.2";
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

        private void btnOk_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

            string from = txtFrom.Text;
            string to = txtTo.Text;

            if (!ValidateIpRange(from, to))
                return;
            else
            {
                txtFrom.Enabled=txtTo.Enabled=false;
                btnCancel.Enabled=btnOk.Enabled=false;
                startIp = txtFrom.Text.Trim();
                endIp = txtTo.Text.Trim();
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                NetworkMapper.InsertToDB(startIp, endIp);
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                MessageBox.Show("محدوده آی‌پی با موفقیت اسکن شد.");
                txtFrom.Enabled = txtTo.Enabled = true;
                btnCancel.Enabled = btnOk.Enabled = true;
            }
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
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
