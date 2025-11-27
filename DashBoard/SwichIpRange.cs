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
            if (txtFrom.Text.Trim().Length < 7 || txtTo.Text.Trim().Length < 7)
            {
                MessageBox.Show("محدوده آی‌پی وارد شده معتبر نیست.");
                return;
            }
            else
            {
                startIp = txtFrom.Text.Trim();
                endIp = txtTo.Text.Trim();
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                NetworkMapper.InsertToDB(startIp, endIp);
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                MessageBox.Show("محدوده آی‌پی با موفقیت ثبت شد.");
                this.Close();
            }
        }
    }
}
