using PcInfoWin.Properties;
using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PcInfoWin
{
    public partial class PcCodeForm : Form
    {
        public static bool IsEditMode = false;
        public string PcCode { get; set; } = string.Empty;

        public static bool resultImportData = false;

        public static PcCodeInfo _pcCodeInfo = new PcCodeInfo();

        private readonly string password = "123";
        public PcCodeForm()
        {

            InitializeComponent();
            this.AcceptButton = btnOk;
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
            if (!IsEditMode)
            {
                this.Close();
                return;
            }
            else
            {
                MessageBox.Show("داده ای وارد نشد و برنامه به طور کامل بسته میشود", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "آیا مطمئن هستید که می‌خواهید ادامه دهید؟",
                "تأیید عملیات",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Yes)
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
                                //var pcCodes = dataSelectHelper.GetAllPcCodes() ?? new List<string>();
                                if ((dataSelectHelper.GetAllPcCodes() ?? new List<string>()).Contains(txtPcCode.Text.Trim()))
                                {
                                    MessageBox.Show("این PC_Code قبلا ثبت شده است. لطفا یک PC_Code دیگر وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                                PcCode = txtPcCode.Text.Trim();
                                _pcCodeInfo.PcCode = PcCode;
                                _pcCodeInfo.PersonnelCode = string.IsNullOrWhiteSpace(txt_UserPersonnelCode.Text.Trim()) ? 0 : int.Parse(txt_UserPersonnelCode.Text.Trim());
                                _pcCodeInfo.UserFullName = txt_UserFullName.Text.Trim();
                                _pcCodeInfo.Unit = txt_Unit.Text.Trim();
                                _pcCodeInfo.Desc1 = txt_Desc1.Text.Trim();
                                _pcCodeInfo.Desc2 = txt_Desc2.Text.Trim();
                                _pcCodeInfo.Desc3 = txt_Desc3.Text.Trim();

                                resultImportData = true;
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
            else
            {

                return;
            }






        
        }
        private void PcCodeForm_Load(object sender, EventArgs e)
        {


        }

        private void txt_UserPersonnelCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // یعنی اجازه ورود نده
            }
        }
    }
}
