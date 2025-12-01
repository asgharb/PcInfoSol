namespace DashBoard
{
    partial class FrmSwitchInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSwitchInfo));
            panel1 = new System.Windows.Forms.Panel();
            btnRefreshInfo = new ReaLTaiizor.Controls.ParrotButton();
            panel2 = new System.Windows.Forms.Panel();
            gridControl_1 = new DevExpress.XtraGrid.GridControl();
            gridView_1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl_1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView_1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(btnRefreshInfo);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1287, 68);
            panel1.TabIndex = 0;
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
            btnRefreshInfo.Location = new System.Drawing.Point(12, 9);
            btnRefreshInfo.Name = "btnRefreshInfo";
            btnRefreshInfo.Size = new System.Drawing.Size(134, 50);
            btnRefreshInfo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            btnRefreshInfo.TabIndex = 1;
            btnRefreshInfo.TextColor = System.Drawing.Color.DodgerBlue;
            btnRefreshInfo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            btnRefreshInfo.Vertical_Alignment = System.Drawing.StringAlignment.Center;
            // 
            // panel2
            // 
            panel2.Controls.Add(gridControl_1);
            panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(0, 68);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(1287, 586);
            panel2.TabIndex = 1;
            // 
            // gridControl_1
            // 
            gridControl_1.Dock = System.Windows.Forms.DockStyle.Fill;
            gridControl_1.Location = new System.Drawing.Point(0, 0);
            gridControl_1.MainView = gridView_1;
            gridControl_1.Name = "gridControl_1";
            gridControl_1.Size = new System.Drawing.Size(1287, 586);
            gridControl_1.TabIndex = 3;
            gridControl_1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView_1 });
            // 
            // gridView1
            // 
            gridView_1.GridControl = gridControl_1;
            gridView_1.Name = "gridView1";
            // 
            // FrmSwitchInfo
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1287, 654);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "FrmSwitchInfo";
            Text = "FrmSwitchInfo";
            Load += FrmSwitchInfo_Load;
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl_1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView_1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private DevExpress.XtraGrid.GridControl gridControl_1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView_1;
        private ReaLTaiizor.Controls.ParrotButton btnRefreshInfo;
    }
}