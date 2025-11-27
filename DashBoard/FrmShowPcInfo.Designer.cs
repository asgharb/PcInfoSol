namespace DashBoard
{
    partial class FrmShowPcInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmShowPcInfo));
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            panel1 = new System.Windows.Forms.Panel();
            btnRefreshMac = new ReaLTaiizor.Controls.ParrotButton();
            btnRefreshInfo = new ReaLTaiizor.Controls.ParrotButton();
            panel2 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            gridControl1.Location = new System.Drawing.Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new System.Drawing.Size(1426, 598);
            gridControl1.TabIndex = 2;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnRefreshMac);
            panel1.Controls.Add(btnRefreshInfo);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1426, 67);
            panel1.TabIndex = 3;
            // 
            // btnRefreshMac
            // 
            btnRefreshMac.BackgroundColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnRefreshMac.ButtonImage = (System.Drawing.Image)resources.GetObject("btnRefreshMac.ButtonImage");
            btnRefreshMac.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnRefreshMac.ButtonText = "RefreshMac";
            btnRefreshMac.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnRefreshMac.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.CornerRadius = 5;
            btnRefreshMac.Horizontal_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshMac.HoverBackgroundColor = System.Drawing.Color.FromArgb(225, 225, 225);
            btnRefreshMac.HoverTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnRefreshMac.Location = new System.Drawing.Point(151, 8);
            btnRefreshMac.Name = "btnRefreshMac";
            btnRefreshMac.Size = new System.Drawing.Size(134, 50);
            btnRefreshMac.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnRefreshMac.TabIndex = 0;
            btnRefreshMac.TextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnRefreshMac.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshMac.Click += btnRefreshMac_Click;
            // 
            // btnRefreshInfo
            // 
            btnRefreshInfo.BackgroundColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnRefreshInfo.ButtonImage = (System.Drawing.Image)resources.GetObject("btnRefreshInfo.ButtonImage");
            btnRefreshInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnRefreshInfo.ButtonText = "Refresh";
            btnRefreshInfo.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnRefreshInfo.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshInfo.CornerRadius = 5;
            btnRefreshInfo.Horizontal_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshInfo.HoverBackgroundColor = System.Drawing.Color.FromArgb(225, 225, 225);
            btnRefreshInfo.HoverTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshInfo.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnRefreshInfo.Location = new System.Drawing.Point(11, 8);
            btnRefreshInfo.Name = "btnRefreshInfo";
            btnRefreshInfo.Size = new System.Drawing.Size(134, 50);
            btnRefreshInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnRefreshInfo.TabIndex = 0;
            btnRefreshInfo.TextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnRefreshInfo.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshInfo.Click += btnRefreshInfo_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(gridControl1);
            panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(0, 67);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(1426, 598);
            panel2.TabIndex = 4;
            // 
            // FrmShowPcInfo
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1426, 665);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "FrmShowPcInfo";
            Text = "FrmShowPcInfo";
            Load += FrmShowPcInfo_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private ReaLTaiizor.Controls.ParrotButton btnRefreshMac;
        private ReaLTaiizor.Controls.ParrotButton btnRefreshInfo;
    }
}