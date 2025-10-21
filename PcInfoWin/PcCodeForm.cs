using PcInfoWin.Properties;
using System;
using System.Windows.Forms;

namespace PcInfoWin
{
    public partial class PcCodeForm : Form
    {
        public static bool IsEditMode = false;
        public string PcCode { get; set; } = string.Empty;

        public static bool resultImportData=false;

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
            try
            {
                SqlDataExtention.Data.DataSelectHelper dataSelectHelper = new SqlDataExtention.Data.DataSelectHelper();

                if (!IsEditMode)
                {
                    Environment.Exit(0);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(txtPassword.Text) && txtPassword.Text == password)
                    {
                        if (!string.IsNullOrWhiteSpace(txtPcCode.Text))
                        {
                            if(dataSelectHelper.GetAllPcCodes().Contains( txtPcCode.Text.Trim()))
                            {
                                MessageBox.Show("این PC_Code قبلا ثبت شده است. لطفا یک PC_Code دیگر وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            PcCode = txtPcCode.Text.Trim();
                            Settings.Default.PcCode = PcCode;
                            Settings.Default.PersonnelCode = string.IsNullOrWhiteSpace(txt_UserPersonnelCode.Text.Trim()) ? 0 : int.Parse(txt_UserPersonnelCode.Text.Trim());
                            Settings.Default.UserFullName = txt_UserFullName.Text.Trim();
                            Settings.Default.Unit = txt_Unit.Text.Trim();
                            Settings.Default.Desc1 = txt_Desc1.Text.Trim();
                            Settings.Default.Desc2 = txt_Desc2.Text.Trim();
                            Settings.Default.Desc3 = txt_Desc3.Text.Trim();
                            resultImportData=true;
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
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ثبت اطلاعات: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PcCodeForm_Load(object sender, EventArgs e)
        {

            
        }
    }
}
