using System.Drawing;
using System.Windows.Forms;

namespace DashBoard
{
    partial class FrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            pnlParent = new Panel();
            pictureBox1 = new PictureBox();
            pnlHead = new Panel();
            lblVersion = new Label();
            nightControlBox1 = new ReaLTaiizor.Controls.NightControlBox();
            Timer_Sidebar_Menu = new Timer(components);
            SideBar = new Panel();
            panel1 = new Panel();
            picMenu = new PictureBox();
            pnlPcInfo = new Panel();
            btnShowPcInfo = new ReaLTaiizor.Controls.ParrotButton();
            pnlSwitchInfo = new Panel();
            btnSwithchInfo = new ReaLTaiizor.Controls.ParrotButton();
            pnlSendMsg = new Panel();
            btnSend = new ReaLTaiizor.Controls.ParrotButton();
            pnlParent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            pnlHead.SuspendLayout();
            SideBar.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picMenu).BeginInit();
            pnlPcInfo.SuspendLayout();
            pnlSwitchInfo.SuspendLayout();
            pnlSendMsg.SuspendLayout();
            SuspendLayout();
            // 
            // pnlParent
            // 
            pnlParent.BackColor = SystemColors.GradientInactiveCaption;
            pnlParent.Controls.Add(pictureBox1);
            pnlParent.Dock = DockStyle.Fill;
            pnlParent.Location = new Point(135, 31);
            pnlParent.Name = "pnlParent";
            pnlParent.Size = new Size(1262, 565);
            pnlParent.TabIndex = 17;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(317, 95);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(588, 435);
            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // pnlHead
            // 
            pnlHead.BackColor = Color.FromArgb(41, 128, 185);
            pnlHead.Controls.Add(lblVersion);
            pnlHead.Controls.Add(nightControlBox1);
            pnlHead.Dock = DockStyle.Top;
            pnlHead.Location = new Point(135, 0);
            pnlHead.Name = "pnlHead";
            pnlHead.Size = new Size(1262, 31);
            pnlHead.TabIndex = 15;
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVersion.ForeColor = Color.White;
            lblVersion.Location = new Point(6, 5);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(68, 20);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Vaersion:";
            // 
            // nightControlBox1
            // 
            nightControlBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            nightControlBox1.BackColor = Color.Transparent;
            nightControlBox1.CloseHoverColor = Color.FromArgb(199, 80, 80);
            nightControlBox1.CloseHoverForeColor = Color.White;
            nightControlBox1.DefaultLocation = true;
            nightControlBox1.DisableMaximizeColor = Color.FromArgb(105, 105, 105);
            nightControlBox1.DisableMinimizeColor = Color.FromArgb(105, 105, 105);
            nightControlBox1.EnableCloseColor = Color.FromArgb(160, 160, 160);
            nightControlBox1.EnableMaximizeButton = true;
            nightControlBox1.EnableMaximizeColor = Color.FromArgb(160, 160, 160);
            nightControlBox1.EnableMinimizeButton = true;
            nightControlBox1.EnableMinimizeColor = Color.FromArgb(160, 160, 160);
            nightControlBox1.Location = new Point(1123, 0);
            nightControlBox1.MaximizeHoverColor = Color.FromArgb(15, 255, 255, 255);
            nightControlBox1.MaximizeHoverForeColor = Color.White;
            nightControlBox1.MinimizeHoverColor = Color.FromArgb(15, 255, 255, 255);
            nightControlBox1.MinimizeHoverForeColor = Color.White;
            nightControlBox1.Name = "nightControlBox1";
            nightControlBox1.Size = new Size(139, 31);
            nightControlBox1.TabIndex = 0;
            // 
            // Timer_Sidebar_Menu
            // 
            Timer_Sidebar_Menu.Interval = 10;
            Timer_Sidebar_Menu.Tick += Timer_Sidebar_Menu_Tick_1;
            // 
            // SideBar
            // 
            SideBar.BackColor = Color.FromArgb(33, 37, 41);
            SideBar.Controls.Add(panel1);
            SideBar.Controls.Add(pnlPcInfo);
            SideBar.Controls.Add(pnlSwitchInfo);
            SideBar.Controls.Add(pnlSendMsg);
            SideBar.Dock = DockStyle.Left;
            SideBar.Location = new Point(0, 0);
            SideBar.MaximumSize = new Size(135, 1647);
            SideBar.MinimumSize = new Size(62, 500);
            SideBar.Name = "SideBar";
            SideBar.Size = new Size(135, 596);
            SideBar.TabIndex = 16;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(243, 112, 33);
            panel1.Controls.Add(picMenu);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(135, 31);
            panel1.TabIndex = 16;
            // 
            // picMenu
            // 
            picMenu.Image = (Image)resources.GetObject("picMenu.Image");
            picMenu.Location = new Point(12, 2);
            picMenu.Name = "picMenu";
            picMenu.Size = new Size(38, 28);
            picMenu.SizeMode = PictureBoxSizeMode.StretchImage;
            picMenu.TabIndex = 0;
            picMenu.TabStop = false;
            picMenu.Click += picMenu_Click;
            // 
            // pnlPcInfo
            // 
            pnlPcInfo.Controls.Add(btnShowPcInfo);
            pnlPcInfo.Location = new Point(3, 98);
            pnlPcInfo.Name = "pnlPcInfo";
            pnlPcInfo.Size = new Size(130, 53);
            pnlPcInfo.TabIndex = 15;
            // 
            // btnShowPcInfo
            // 
            btnShowPcInfo.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnShowPcInfo.ButtonImage = (Image)resources.GetObject("btnShowPcInfo.ButtonImage");
            btnShowPcInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnShowPcInfo.ButtonText = "Pc Info";
            btnShowPcInfo.ClickBackColor = Color.Transparent;
            btnShowPcInfo.ClickTextColor = Color.White;
            btnShowPcInfo.CornerRadius = 5;
            btnShowPcInfo.Dock = DockStyle.Fill;
            btnShowPcInfo.Font = new Font("Segoe UI", 11.25F);
            btnShowPcInfo.Horizontal_Alignment = StringAlignment.Center;
            btnShowPcInfo.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnShowPcInfo.HoverTextColor = Color.White;
            btnShowPcInfo.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnShowPcInfo.Location = new Point(0, 0);
            btnShowPcInfo.Name = "btnShowPcInfo";
            btnShowPcInfo.Size = new Size(130, 53);
            btnShowPcInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnShowPcInfo.TabIndex = 10;
            btnShowPcInfo.TextColor = Color.White;
            btnShowPcInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnShowPcInfo.Vertical_Alignment = StringAlignment.Center;
            btnShowPcInfo.Click += btnShowPcInfo_Click;
            // 
            // pnlSwitchInfo
            // 
            pnlSwitchInfo.Controls.Add(btnSwithchInfo);
            pnlSwitchInfo.Location = new Point(3, 246);
            pnlSwitchInfo.Name = "pnlSwitchInfo";
            pnlSwitchInfo.Size = new Size(130, 53);
            pnlSwitchInfo.TabIndex = 15;
            // 
            // btnSwithchInfo
            // 
            btnSwithchInfo.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnSwithchInfo.ButtonImage = (Image)resources.GetObject("btnSwithchInfo.ButtonImage");
            btnSwithchInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnSwithchInfo.ButtonText = "Swithch Info";
            btnSwithchInfo.ClickBackColor = Color.Transparent;
            btnSwithchInfo.ClickTextColor = Color.White;
            btnSwithchInfo.CornerRadius = 5;
            btnSwithchInfo.Dock = DockStyle.Fill;
            btnSwithchInfo.Font = new Font("Segoe UI", 11.25F);
            btnSwithchInfo.Horizontal_Alignment = StringAlignment.Center;
            btnSwithchInfo.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnSwithchInfo.HoverTextColor = Color.White;
            btnSwithchInfo.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnSwithchInfo.Location = new Point(0, 0);
            btnSwithchInfo.Name = "btnSwithchInfo";
            btnSwithchInfo.Size = new Size(130, 53);
            btnSwithchInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnSwithchInfo.TabIndex = 12;
            btnSwithchInfo.TextColor = Color.White;
            btnSwithchInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnSwithchInfo.Vertical_Alignment = StringAlignment.Center;
            btnSwithchInfo.Click += btnSwithchInfo_Click;
            // 
            // pnlSendMsg
            // 
            pnlSendMsg.Controls.Add(btnSend);
            pnlSendMsg.Location = new Point(3, 174);
            pnlSendMsg.Name = "pnlSendMsg";
            pnlSendMsg.Size = new Size(130, 53);
            pnlSendMsg.TabIndex = 14;
            // 
            // btnSend
            // 
            btnSend.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnSend.ButtonImage = (Image)resources.GetObject("btnSend.ButtonImage");
            btnSend.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnSend.ButtonText = "Send Msg";
            btnSend.ClickBackColor = Color.Transparent;
            btnSend.ClickTextColor = Color.White;
            btnSend.CornerRadius = 5;
            btnSend.Dock = DockStyle.Fill;
            btnSend.Font = new Font("Segoe UI", 11.25F);
            btnSend.Horizontal_Alignment = StringAlignment.Center;
            btnSend.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnSend.HoverTextColor = Color.White;
            btnSend.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnSend.Location = new Point(0, 0);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(130, 53);
            btnSend.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnSend.TabIndex = 12;
            btnSend.TextColor = Color.White;
            btnSend.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnSend.Vertical_Alignment = StringAlignment.Center;
            btnSend.Click += btnSend_Click;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1397, 596);
            Controls.Add(pnlParent);
            Controls.Add(pnlHead);
            Controls.Add(SideBar);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FrmMain";
            Text = "FrmMain";
            Load += FrmMain_Load;
            pnlParent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            pnlHead.ResumeLayout(false);
            pnlHead.PerformLayout();
            SideBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picMenu).EndInit();
            pnlPcInfo.ResumeLayout(false);
            pnlSwitchInfo.ResumeLayout(false);
            pnlSendMsg.ResumeLayout(false);
            ResumeLayout(false);


        }

        #endregion

        private Panel pnlParent;
        private PictureBox pictureBox1;
        private Panel pnlHead;
        private ReaLTaiizor.Controls.NightControlBox nightControlBox1;
        private Timer Timer_Sidebar_Menu;
        private Panel SideBar;
        private Panel pnlPcInfo;
        private ReaLTaiizor.Controls.ParrotButton btnShowPcInfo;
        private Panel pnlSwitchInfo;
        private ReaLTaiizor.Controls.ParrotButton btnSwithchInfo;
        private Panel pnlSendMsg;
        private ReaLTaiizor.Controls.ParrotButton btnSend;
        private Label lblVersion;
        private Panel panel1;
        private PictureBox picMenu;
    }
}