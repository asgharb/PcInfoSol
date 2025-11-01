using PcInfoWin.Properties;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PcInfoWin
{
    public partial class PcCodeForm : Form
    {
        public static bool IsNewMode = false;
        public static bool IsEditMode = false;
        public string PcCode { get; set; } = string.Empty;

        public static bool resultImportData = false;

        public static PcCodeInfo _pcCodeInfo = new PcCodeInfo();

        private readonly string password = "123";


        private bool _userClosing = false;
        private void PcCodeForm_Load(object sender, EventArgs e)
        {
            DataSelectHelper dataSelectHelper = new DataSelectHelper();
            cmbUnit.DataSource = dataSelectHelper.SelectAllWitoutConditonal<Department>();
            cmbUnit.DisplayMember = "Name";
            cmbUnit.ValueMember = "Id";
            if (IsEditMode)
            {
                txtPcCode.Text = _pcCodeInfo.PcCode;
                txt_Desc1.Text = _pcCodeInfo.Desc1;
                txt_Desc2.Text = _pcCodeInfo.Desc2;
                txt_Desc3.Text = _pcCodeInfo.Desc3;
                txt_UserFullName.Text = _pcCodeInfo.UserFullName;
                txt_UserPersonnelCode.Text = _pcCodeInfo.PersonnelCode.ToString();
                cmbUnit.SelectedIndex = cmbUnit.FindStringExact(_pcCodeInfo.Unit);
            }

        }
        public PcCodeForm()
        {
            InitializeComponent();

            this.FormClosing += MainForm_FormClosing;
            this.KeyPreview = true; // برای Alt+F4 در KeyDown
            this.KeyDown += MainForm_KeyDown;


            this.AcceptButton = btnOk;
            if (!IsEditMode && !IsNewMode)
            {
                btnOk.Text = "خروج";
                panel2.Visible = false;
                this.SuspendLayout();
                this.Width = 400;
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
            if (!IsEditMode && !IsNewMode)
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

            if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text != password)
            {
                MessageBox.Show("رمز عبور اشتباه است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
                    if (!IsEditMode && !IsNewMode)
                    {
                        Application.Exit();
                    }
                    else
                    {
                        SqlDataExtention.Data.DataSelectHelper dataSelectHelper = new SqlDataExtention.Data.DataSelectHelper();

                        if (!string.IsNullOrWhiteSpace(txtPcCode.Text))
                        {
                            if ((dataSelectHelper.GetAllPcCodes() ?? new List<string>()).Contains(txtPcCode.Text.Trim()))
                            {
                                if (IsNewMode || (IsEditMode && txtPcCode.Text.Trim() != _pcCodeInfo.PcCode))
                                {
                                    MessageBox.Show("این PC_Code قبلا ثبت شده است. لطفا یک PC_Code دیگر وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            PcCode = txtPcCode.Text.Trim();
                            _pcCodeInfo.PcCode = PcCode;
                            _pcCodeInfo.PersonnelCode = string.IsNullOrWhiteSpace(txt_UserPersonnelCode.Text.Trim()) ? 0 : int.Parse(txt_UserPersonnelCode.Text.Trim());
                            _pcCodeInfo.UserFullName = txt_UserFullName.Text.Trim();
                            _pcCodeInfo.Unit = ((Department)cmbUnit.SelectedItem).Name;
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
                            resultImportData = false;
                        }

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطا در ثبت اطلاعات: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    resultImportData = false;
                    IsEditMode = IsNewMode = false;
                }
            }
            else
            {
                resultImportData = false;
                IsEditMode = IsNewMode = false;
                return;
            }
        }

        private void txt_UserPersonnelCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // یعنی اجازه ورود نده
            }
        }



        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // فقط وقتی کاربر ضربدر زد یا Alt+F4 زده
            if (e.CloseReason == CloseReason.UserClosing && _userClosing)
            {
                resultImportData = false;
                IsEditMode = IsNewMode = false;
            }
            else
            {
                // سایر حالت‌ها مثل this.Close() یا ShowDialog تمام شدن
                _userClosing = false; // reset flag
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.F4)
            {
                _userClosing = true;
            }
        }
    }
}
