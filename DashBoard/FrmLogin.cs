using DevExpress.XtraEditors.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class FrmLogin : Form
    {
        private Color colorPrimary = Color.FromArgb(41, 128, 185); // آبی مدرن
        private Color colorText = Color.FromArgb(64, 64, 64);
        public FrmLogin()
        {
            InitializeComponent();
            this.SuspendLayout();
            this.Name = "LoginForm";
            this.ResumeLayout(false);
            SetupModernDesign();
        }

        public static string User = "";
        private void SetupModernDesign()
        {
            // تنظیمات اصلی فرم
            this.FormBorderStyle = FormBorderStyle.None; // حذف حاشیه ویندوز
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(750, 500);
            this.BackColor = Color.White;

            // --- پنل سمت چپ (لوگو و خوش‌آمدگویی) ---
            Panel panelLeft = new Panel();
            panelLeft.Dock = DockStyle.Left;
            panelLeft.Width = 300;
            panelLeft.BackColor = colorPrimary;
            this.Controls.Add(panelLeft);

            PictureBox picLogo = new PictureBox(); 
            picLogo.Size = new Size(100, 100);
            picLogo.Location = new Point(100, 90);
            picLogo.Image = Properties.Resources.security_2;
            picLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            panelLeft.Controls.Add(picLogo);

            Label lblWelcome = new Label();
            lblWelcome.Text = "Welcome to\nNetwork Manager";
            lblWelcome.ForeColor = Color.White;
            lblWelcome.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblWelcome.AutoSize = true;
            //lblWelcome.TextAlign = ContentAlignment.TopRight;
            lblWelcome.Location = new Point(10, 250);
            panelLeft.Controls.Add(lblWelcome);

            // --- دکمه بستن (X) ---
            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Arial", 12, FontStyle.Bold);
            btnClose.ForeColor = colorPrimary;
            btnClose.BackColor = Color.White;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(40, 40);
            btnClose.Location = new Point(710, 0);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnClose);

            // --- عنوان صفحه لاگین ---
            Label lblLoginTitle = new Label();
            lblLoginTitle.Text = "Login into your account";
            lblLoginTitle.ForeColor = colorPrimary;
            lblLoginTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblLoginTitle.AutoSize = true;
            lblLoginTitle.Location = new Point(340, 50);
            this.Controls.Add(lblLoginTitle);

            // --- ورودی نام کاربری ---
            CreateInput(this, "Username", 340, 140, false);

            // --- ورودی رمز عبور ---
            CreateInput(this, "Password", 340, 220, true);

            // --- دکمه ورود ---
            Button btnLogin = new Button();
            btnLogin.Text = "LOGIN";
            btnLogin.BackColor = colorPrimary;
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnLogin.Size = new Size(150, 45);
            btnLogin.Location = new Point(345, 320);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            // قابلیت جابجایی فرم (چون نوار عنوان ندارد)
            MakeDraggable(this);
            MakeDraggable(panelLeft);
        }

        private void CreateInput(Control parent, string labelText, int x, int y, bool isPassword)
        {
            // لیبل بالای تکست باکس
            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.ForeColor = colorPrimary;
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            parent.Controls.Add(lbl);

            // خود تکست باکس
            TextBox txt = new TextBox();
            txt.BorderStyle = BorderStyle.None; // حذف کادر زشت پیش‌فرض
            txt.Font = new Font("Segoe UI", 12);
            txt.ForeColor = colorText;
            txt.Location = new Point(x, y + 25);
            txt.Width = 350;
            txt.Name = "txt" + labelText; // برای دسترسی راحت‌تر
            if (isPassword) txt.UseSystemPasswordChar = true;
            parent.Controls.Add(txt);
            if(labelText == "Username") txt.Text = "IT";

            // *** این را اضافه کنید ***
            txt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnLogin_Click(null, null);
                }
            };
            // خط رنگی زیر تکست باکس
            Panel line = new Panel();
            line.Size = new Size(350, 2); // ارتفاع خط 2 پیکسل
            line.BackColor = colorPrimary;
            line.Location = new Point(x, y + 50);
            parent.Controls.Add(line);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string userInput = this.Controls["txtUsername"].Text;
            string passInput = this.Controls["txtPassword"].Text;

            bool isAuthenticated = false;

            if (userInput.ToLower() == "admin" && passInput == "12369!@#")
            {
                isAuthenticated = true;
                User = "admin"; 
            }
            else if (userInput.ToLower() == "it" && passInput == "123!")
            {
                isAuthenticated = true;
                User = "it"; 
            }

            if (isAuthenticated)
            {

                this.DialogResult = DialogResult.OK;
                this.Close(); 
            }
            else
            {
                MessageBox.Show("Invalid Username or Password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // --- کدهای مربوط به جابجایی فرم (Drag & Drop) ---
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);

        private void MakeDraggable(Control c)
        {
            c.MouseDown += (s, e) =>
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0x112, 0xf012, 0);
            };
        }
    }
}