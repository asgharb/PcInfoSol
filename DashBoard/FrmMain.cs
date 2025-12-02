using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.VirtualKeyboard;

namespace DashBoard
{
    public partial class FrmMain : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        bool sideBar_Expand = true;

        private Form activeForm = null; // فرم فعلی باز شده
        public FrmMain()
        {
            InitializeComponent();

            pnlHead.MouseDown += pnlHead_MouseDown;
            pnlHead.MouseMove += pnlHead_MouseMove;
            pnlHead.MouseUp += pnlHead_MouseUp;

            this.FormClosed += FrmMain_FormClosed;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            lblVersion.Text = "Version: " + Application.ProductVersion.Split('+')[0];

            bool isAdminUser = !string.IsNullOrWhiteSpace(FrmLogin.User) &&
                               FrmLogin.User.ToLower() == "admin";
            if (isAdminUser)
            {
                btnSend.Enabled = true;
            }
            else
            {
                btnSend.Enabled = false;
            }
            CenterPictureBox();

        }


        private void OpenChildForm(Form childForm)
        {
            // اگر فرمی باز است، آن را ببند
            if (activeForm != null)
                activeForm.Close();

            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill; // اندازه دقیقاً به اندازه پنل
            pnlParent.Controls.Clear(); // اطمینان از پاک بودن پنل
            pnlParent.Controls.Add(childForm);
            pnlParent.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        private void Timer_Sidebar_Menu_Tick(object sender, EventArgs e)
        {
            if (sideBar_Expand)
            {
                SideBar.Width -= 10;
                if (SideBar.Width == SideBar.MinimumSize.Width)
                {
                    sideBar_Expand = false;
                    Timer_Sidebar_Menu.Stop();
                }
            }
            else
            {
                SideBar.Width += 10;
                if (SideBar.Width == SideBar.MaximumSize.Width)
                {
                    sideBar_Expand = true;
                    Timer_Sidebar_Menu.Stop();
                }
            }
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            Timer_Sidebar_Menu.Start();
        }

        private void Timer_Sidebar_Menu_Tick_1(object sender, EventArgs e)
        {
            if (sideBar_Expand)
            {
                SideBar.Width -= 10;
                if (SideBar.Width == SideBar.MinimumSize.Width)
                {
                    sideBar_Expand = false;
                    Timer_Sidebar_Menu.Stop();
                }
            }
            else
            {
                SideBar.Width += 10;
                if (SideBar.Width == SideBar.MaximumSize.Width)
                {
                    sideBar_Expand = true;
                    Timer_Sidebar_Menu.Stop();
                }
            }
        }

        private void btnShowPcInfo_Click(object sender, EventArgs e)
        {

            btnShowPcInfo.BackgroundColor = Color.FromArgb(243, 112, 33);
            pnlPcInfo.BackColor = Color.FromArgb(243, 112, 33);

            pnlSendMsg.BackColor = Color.FromArgb(33, 37, 41);
            btnSend.BackgroundColor = Color.FromArgb(33, 37, 41);

            btnSwithchInfo.BackgroundColor = Color.FromArgb(33, 37, 41);
            pnlSwitchInfo.BackColor = Color.FromArgb(33, 37, 41);

            if (activeForm is FrmShowPcInfo)
                return;

            OpenChildForm(new FrmShowPcInfo());
        }

        private void pnlHead_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                // ذخیره موقعیت موس و فرم
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }

        private void pnlHead_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                // محاسبه اختلاف حرکت موس
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                // جابه‌جایی فرم
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void pnlHead_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void CenterPictureBox()
        {
            pictureBox1.Left = (pnlParent.Width - pictureBox1.Width) / 2;
            pictureBox1.Top = (pnlParent.Height - pictureBox1.Height) / 2;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

            btnShowPcInfo.BackgroundColor = Color.FromArgb(33, 37, 41);
            pnlPcInfo.BackColor = Color.FromArgb(33, 37, 41);

            pnlSendMsg.BackColor = Color.FromArgb(243, 112, 33);
            btnSend.BackgroundColor = Color.FromArgb(243, 112, 33);

            btnSwithchInfo.BackgroundColor = Color.FromArgb(33, 37, 41);
            pnlSwitchInfo.BackColor = Color.FromArgb(33, 37, 41);

            if (activeForm is FrmSendMsg)
                return;

            OpenChildForm(new FrmSendMsg());
        }

        private void btnSwithchInfo_Click(object sender, EventArgs e)
        {
            btnShowPcInfo.BackgroundColor = Color.FromArgb(33, 37, 41);
            pnlPcInfo.BackColor = Color.FromArgb(33, 37, 41);

            pnlSendMsg.BackColor = Color.FromArgb(33, 37, 41);
            btnSend.BackgroundColor = Color.FromArgb(33, 37, 41);

            btnSwithchInfo.BackgroundColor = Color.FromArgb(243, 112, 33);
            pnlSwitchInfo.BackColor = Color.FromArgb(243, 112, 33);

            if (activeForm is FrmSwitchInfo)
                return;

            OpenChildForm(new FrmSwitchInfo());
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            // وقتی فرم بسته شد، کل برنامه بسته شود
            Application.Exit();
        }

        private void picMenu_Click(object sender, EventArgs e)
        {
            Timer_Sidebar_Menu.Start();
        }
    }
}
