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
            nightControlBox1 = new ReaLTaiizor.Controls.NightControlBox();
            Timer_Sidebar_Menu = new Timer(components);
            SideBar = new Panel();
            panel5 = new Panel();
            pnlSideInbox = new Panel();
            btnShowPcInfo = new ReaLTaiizor.Controls.ParrotButton();
            panel3 = new Panel();
            pnlSideSwitch = new Panel();
            btnSwithchInfo = new ReaLTaiizor.Controls.ParrotButton();
            panel2 = new Panel();
            pnlSideSend = new Panel();
            btnSend = new ReaLTaiizor.Controls.ParrotButton();
            btnMenu = new ReaLTaiizor.Controls.ParrotButton();
            pnlParent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            pnlHead.SuspendLayout();
            SideBar.SuspendLayout();
            panel5.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // pnlParent
            // 
            pnlParent.BackColor = SystemColors.ActiveCaption;
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
            pnlHead.BackColor = Color.FromArgb(56, 142, 60);
            pnlHead.Controls.Add(nightControlBox1);
            pnlHead.Dock = DockStyle.Top;
            pnlHead.Location = new Point(135, 0);
            pnlHead.Name = "pnlHead";
            pnlHead.Size = new Size(1262, 31);
            pnlHead.TabIndex = 15;
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
            SideBar.BackColor = Color.FromArgb(35, 40, 45);
            SideBar.Controls.Add(panel5);
            SideBar.Controls.Add(panel3);
            SideBar.Controls.Add(panel2);
            SideBar.Controls.Add(btnMenu);
            SideBar.Dock = DockStyle.Left;
            SideBar.Location = new Point(0, 0);
            SideBar.MaximumSize = new Size(135, 1647);
            SideBar.MinimumSize = new Size(62, 500);
            SideBar.Name = "SideBar";
            SideBar.Size = new Size(135, 596);
            SideBar.TabIndex = 16;
            // 
            // panel5
            // 
            panel5.Controls.Add(pnlSideInbox);
            panel5.Controls.Add(btnShowPcInfo);
            panel5.Location = new Point(3, 95);
            panel5.Name = "panel5";
            panel5.Size = new Size(130, 53);
            panel5.TabIndex = 15;
            // 
            // pnlSideInbox
            // 
            pnlSideInbox.BackColor = Color.FromArgb(56, 142, 60);
            pnlSideInbox.Dock = DockStyle.Left;
            pnlSideInbox.Location = new Point(0, 0);
            pnlSideInbox.Name = "pnlSideInbox";
            pnlSideInbox.Size = new Size(3, 53);
            pnlSideInbox.TabIndex = 13;
            // 
            // btnShowPcInfo
            // 
            btnShowPcInfo.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnShowPcInfo.ButtonImage = (Image)resources.GetObject("btnShowPcInfo.ButtonImage");
            btnShowPcInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnShowPcInfo.ButtonText = "Pc Info";
            btnShowPcInfo.ClickBackColor = Color.FromArgb(35, 40, 45);
            btnShowPcInfo.ClickTextColor = Color.White;
            btnShowPcInfo.CornerRadius = 5;
            btnShowPcInfo.Font = new Font("Segoe UI", 11.25F);
            btnShowPcInfo.Horizontal_Alignment = StringAlignment.Center;
            btnShowPcInfo.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnShowPcInfo.HoverTextColor = Color.White;
            btnShowPcInfo.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnShowPcInfo.Location = new Point(8, 3);
            btnShowPcInfo.Name = "btnShowPcInfo";
            btnShowPcInfo.Size = new Size(113, 47);
            btnShowPcInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnShowPcInfo.TabIndex = 10;
            btnShowPcInfo.TextColor = Color.White;
            btnShowPcInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnShowPcInfo.Vertical_Alignment = StringAlignment.Center;
            btnShowPcInfo.Click += btnShowPcInfo_Click;
            // 
            // panel3
            // 
            panel3.Controls.Add(pnlSideSwitch);
            panel3.Controls.Add(btnSwithchInfo);
            panel3.Location = new Point(3, 243);
            panel3.Name = "panel3";
            panel3.Size = new Size(130, 53);
            panel3.TabIndex = 15;
            // 
            // pnlSideSwitch
            // 
            pnlSideSwitch.BackColor = Color.FromArgb(56, 142, 60);
            pnlSideSwitch.Dock = DockStyle.Left;
            pnlSideSwitch.Location = new Point(0, 0);
            pnlSideSwitch.Name = "pnlSideSwitch";
            pnlSideSwitch.Size = new Size(3, 53);
            pnlSideSwitch.TabIndex = 13;
            // 
            // btnSwithchInfo
            // 
            btnSwithchInfo.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnSwithchInfo.ButtonImage = (Image)resources.GetObject("btnSwithchInfo.ButtonImage");
            btnSwithchInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnSwithchInfo.ButtonText = "Swithch Info";
            btnSwithchInfo.ClickBackColor = Color.FromArgb(35, 40, 45);
            btnSwithchInfo.ClickTextColor = Color.White;
            btnSwithchInfo.CornerRadius = 5;
            btnSwithchInfo.Font = new Font("Segoe UI", 11.25F);
            btnSwithchInfo.Horizontal_Alignment = StringAlignment.Center;
            btnSwithchInfo.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnSwithchInfo.HoverTextColor = Color.White;
            btnSwithchInfo.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnSwithchInfo.Location = new Point(8, 3);
            btnSwithchInfo.Name = "btnSwithchInfo";
            btnSwithchInfo.Size = new Size(113, 47);
            btnSwithchInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnSwithchInfo.TabIndex = 12;
            btnSwithchInfo.TextColor = Color.White;
            btnSwithchInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnSwithchInfo.Vertical_Alignment = StringAlignment.Center;
            btnSwithchInfo.Click += btnSwithchInfo_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(pnlSideSend);
            panel2.Controls.Add(btnSend);
            panel2.Location = new Point(3, 171);
            panel2.Name = "panel2";
            panel2.Size = new Size(130, 53);
            panel2.TabIndex = 14;
            // 
            // pnlSideSend
            // 
            pnlSideSend.BackColor = Color.FromArgb(56, 142, 60);
            pnlSideSend.Dock = DockStyle.Left;
            pnlSideSend.Location = new Point(0, 0);
            pnlSideSend.Name = "pnlSideSend";
            pnlSideSend.Size = new Size(3, 53);
            pnlSideSend.TabIndex = 13;
            // 
            // btnSend
            // 
            btnSend.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnSend.ButtonImage = (Image)resources.GetObject("btnSend.ButtonImage");
            btnSend.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnSend.ButtonText = "Send Msg";
            btnSend.ClickBackColor = Color.FromArgb(35, 40, 45);
            btnSend.ClickTextColor = Color.White;
            btnSend.CornerRadius = 5;
            btnSend.Font = new Font("Segoe UI", 11.25F);
            btnSend.Horizontal_Alignment = StringAlignment.Center;
            btnSend.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnSend.HoverTextColor = Color.White;
            btnSend.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnSend.Location = new Point(8, 3);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(113, 47);
            btnSend.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnSend.TabIndex = 12;
            btnSend.TextColor = Color.White;
            btnSend.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnSend.Vertical_Alignment = StringAlignment.Center;
            btnSend.Click += btnSend_Click;
            // 
            // btnMenu
            // 
            btnMenu.BackgroundColor = Color.FromArgb(35, 40, 45);
            btnMenu.ButtonImage = (Image)resources.GetObject("btnMenu.ButtonImage");
            btnMenu.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnMenu.ButtonText = "Menu";
            btnMenu.ClickBackColor = Color.FromArgb(35, 40, 45);
            btnMenu.ClickTextColor = Color.White;
            btnMenu.CornerRadius = 5;
            btnMenu.Font = new Font("Segoe UI", 11.25F);
            btnMenu.Horizontal_Alignment = StringAlignment.Center;
            btnMenu.HoverBackgroundColor = Color.FromArgb(35, 40, 45);
            btnMenu.HoverTextColor = Color.White;
            btnMenu.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnMenu.Location = new Point(7, 15);
            btnMenu.Name = "btnMenu";
            btnMenu.Size = new Size(120, 28);
            btnMenu.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnMenu.TabIndex = 9;
            btnMenu.TextColor = Color.White;
            btnMenu.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnMenu.Vertical_Alignment = StringAlignment.Center;
            btnMenu.Click += btnMenu_Click;
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
            SideBar.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);


        }

        #endregion

        private Panel pnlParent;
        private PictureBox pictureBox1;
        private Panel pnlHead;
        private ReaLTaiizor.Controls.NightControlBox nightControlBox1;
        private Timer Timer_Sidebar_Menu;
        private Panel SideBar;
        private Panel panel5;
        private Panel pnlSideInbox;
        private ReaLTaiizor.Controls.ParrotButton btnShowPcInfo;
        private Panel panel3;
        private Panel pnlSideSwitch;
        private ReaLTaiizor.Controls.ParrotButton btnSwithchInfo;
        private Panel panel2;
        private Panel pnlSideSend;
        private ReaLTaiizor.Controls.ParrotButton btnSend;
        private ReaLTaiizor.Controls.ParrotButton btnMenu;
    }
}