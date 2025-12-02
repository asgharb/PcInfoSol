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
            btnRefreshInfo = new ReaLTaiizor.Controls.ParrotButton();
            btnRefreshMac = new ReaLTaiizor.Controls.ParrotButton();
            btnPrev = new ReaLTaiizor.Controls.ParrotButton();
            panel2 = new System.Windows.Forms.Panel();
            panel4 = new System.Windows.Forms.Panel();
            panel3 = new System.Windows.Forms.Panel();
            lblPageInfo = new System.Windows.Forms.Label();
            btnNext = new ReaLTaiizor.Controls.ParrotButton();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            gridControl1.Location = new System.Drawing.Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new System.Drawing.Size(1348, 484);
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
            panel1.Controls.Add(btnRefreshInfo);
            panel1.Controls.Add(btnRefreshMac);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1348, 67);
            panel1.TabIndex = 3;
            // 
            // btnRefreshInfo
            // 
            btnRefreshInfo.BackgroundColor = System.Drawing.SystemColors.Control;
            btnRefreshInfo.ButtonImage = (System.Drawing.Image)resources.GetObject("btnRefreshInfo.ButtonImage");
            btnRefreshInfo.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnRefreshInfo.ButtonText = "Refresh Info";
            btnRefreshInfo.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnRefreshInfo.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshInfo.CornerRadius = 5;
            btnRefreshInfo.Font = new System.Drawing.Font("Segoe UI", 12F);
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
            // btnRefreshMac
            // 
            btnRefreshMac.BackgroundColor = System.Drawing.SystemColors.Control;
            btnRefreshMac.ButtonImage = (System.Drawing.Image)resources.GetObject("btnRefreshMac.ButtonImage");
            btnRefreshMac.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnRefreshMac.ButtonText = "Refresh Switch";
            btnRefreshMac.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnRefreshMac.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.CornerRadius = 5;
            btnRefreshMac.Font = new System.Drawing.Font("Segoe UI", 12F);
            btnRefreshMac.Horizontal_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshMac.HoverBackgroundColor = System.Drawing.Color.FromArgb(225, 225, 225);
            btnRefreshMac.HoverTextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnRefreshMac.Location = new System.Drawing.Point(160, 8);
            btnRefreshMac.Name = "btnRefreshMac";
            btnRefreshMac.Size = new System.Drawing.Size(134, 50);
            btnRefreshMac.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnRefreshMac.TabIndex = 0;
            btnRefreshMac.TextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshMac.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnRefreshMac.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            btnRefreshMac.Click += btnRefreshMac_Click;
            // 
            // btnPrev
            // 
            btnPrev.BackgroundColor = System.Drawing.SystemColors.Control;
            btnPrev.ButtonImage = (System.Drawing.Image)resources.GetObject("btnPrev.ButtonImage");
            btnPrev.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnPrev.ButtonText = "";
            btnPrev.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnPrev.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnPrev.CornerRadius = 5;
            btnPrev.Horizontal_Alignment = System.Drawing.StringAlignment.Center;
            btnPrev.HoverBackgroundColor = System.Drawing.Color.FromArgb(225, 225, 225);
            btnPrev.HoverTextColor = System.Drawing.Color.DodgerBlue;
            btnPrev.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnPrev.Location = new System.Drawing.Point(19, 6);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new System.Drawing.Size(36, 27);
            btnPrev.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnPrev.TabIndex = 0;
            btnPrev.TextColor = System.Drawing.Color.DodgerBlue;
            btnPrev.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnPrev.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            btnPrev.Click += btnPrev_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(panel4);
            panel2.Controls.Add(panel3);
            panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(0, 67);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(1348, 526);
            panel2.TabIndex = 4;
            // 
            // panel4
            // 
            panel4.Controls.Add(gridControl1);
            panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            panel4.Location = new System.Drawing.Point(0, 0);
            panel4.Name = "panel4";
            panel4.Size = new System.Drawing.Size(1348, 484);
            panel4.TabIndex = 4;
            // 
            // panel3
            // 
            panel3.Controls.Add(lblPageInfo);
            panel3.Controls.Add(btnNext);
            panel3.Controls.Add(btnPrev);
            panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel3.Location = new System.Drawing.Point(0, 484);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(1348, 42);
            panel3.TabIndex = 3;
            // 
            // lblPageInfo
            // 
            lblPageInfo.AutoSize = true;
            lblPageInfo.Location = new System.Drawing.Point(49, 13);
            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.Size = new System.Drawing.Size(77, 15);
            lblPageInfo.TabIndex = 1;
            lblPageInfo.Text = "page 10 of 12";
            // 
            // btnNext
            // 
            btnNext.BackgroundColor = System.Drawing.SystemColors.Control;
            btnNext.ButtonImage = (System.Drawing.Image)resources.GetObject("btnNext.ButtonImage");
            btnNext.ButtonStyle = ReaLTaiizor.Controls.ParrotButton.Style.MaterialRounded;
            btnNext.ButtonText = "";
            btnNext.ClickBackColor = System.Drawing.Color.FromArgb(195, 195, 195);
            btnNext.ClickTextColor = System.Drawing.Color.DodgerBlue;
            btnNext.CornerRadius = 5;
            btnNext.Horizontal_Alignment = System.Drawing.StringAlignment.Center;
            btnNext.HoverBackgroundColor = System.Drawing.Color.FromArgb(225, 225, 225);
            btnNext.HoverTextColor = System.Drawing.Color.DodgerBlue;
            btnNext.ImagePosition = ReaLTaiizor.Controls.ParrotButton.ImgPosition.Left;
            btnNext.Location = new System.Drawing.Point(131, 7);
            btnNext.Name = "btnNext";
            btnNext.Size = new System.Drawing.Size(32, 27);
            btnNext.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnNext.TabIndex = 0;
            btnNext.TextColor = System.Drawing.Color.DodgerBlue;
            btnNext.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnNext.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            btnNext.Click += btnNext_Click;
            // 
            // FrmShowPcInfo
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1348, 593);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "FrmShowPcInfo";
            Text = "FrmShowPcInfo";
            Load += FrmShowPcInfo_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private ReaLTaiizor.Controls.ParrotButton btnRefreshMac;
        private ReaLTaiizor.Controls.ParrotButton btnPrev;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel3;
        private ReaLTaiizor.Controls.ParrotButton btnNext;
        private System.Windows.Forms.Label lblPageInfo;
        private ReaLTaiizor.Controls.ParrotButton btnRefreshInfo;
    }
}