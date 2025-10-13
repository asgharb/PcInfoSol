using PcInfoWin.Properties;
using System;
using System.Windows.Forms;

namespace PcInfoWin
{
    public partial class PcCodeForm : Form
    {
        public bool IsEditMode { get; set; } = false;
        public string PcCode { get; set; } = string.Empty;

        private readonly string password = "123";
        public PcCodeForm()
        {

            InitializeComponent();
            if (!IsEditMode)
            {
                btnOk.Text = "خروج";
                panel2.Visible = false;
                this.SuspendLayout();
                this.Height = panel1.Height + panel3.Height + this.Height - this.ClientSize.Height;
                this.ResumeLayout();
            }
            else
            {
                panel2.Visible = true;
                btnOk.Text = "تایید";
            }
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            if(!IsEditMode)
            {
                this.Close();
                return;
            }

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!IsEditMode)
            {
               Environment.Exit(0);
            }
            else
            {
                if(!string.IsNullOrWhiteSpace(txtPassword.Text) && txtPassword.Text== password)
                {
                    if(!string.IsNullOrWhiteSpace(txtPcCode.Text))
                    {
                        PcCode = txtPcCode.Text.Trim();
                        Settings.Default.PcCodeName= PcCode;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("لطفا PC_Code را وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("رمز عبور اشتباه است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PcCodeForm_Load(object sender, EventArgs e)
        {

            
        }
    }
}
