namespace DashBoard
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            ribbonControl1 = new DevExpress.XtraBars.Ribbon.RibbonControl();
            barButtonItem1 = new DevExpress.XtraBars.BarButtonItem();
            btnRefresh = new DevExpress.XtraBars.BarButtonItem();
            btnSendMsg = new DevExpress.XtraBars.BarButtonItem();
            barButtonItem3 = new DevExpress.XtraBars.BarButtonItem();
            ribbonPageCategory1 = new DevExpress.XtraBars.Ribbon.RibbonPageCategory();
            ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            SuspendLayout();
            // 
            // ribbonControl1
            // 
            ribbonControl1.ExpandCollapseItem.Id = 0;
            ribbonControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { ribbonControl1.ExpandCollapseItem, barButtonItem1, btnRefresh, btnSendMsg, barButtonItem3 });
            ribbonControl1.Location = new System.Drawing.Point(0, 0);
            ribbonControl1.MaxItemId = 7;
            ribbonControl1.Name = "ribbonControl1";
            ribbonControl1.PageCategories.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageCategory[] { ribbonPageCategory1 });
            ribbonControl1.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] { ribbonPage1 });
            ribbonControl1.Size = new System.Drawing.Size(1371, 146);
            // 
            // barButtonItem1
            // 
            barButtonItem1.Caption = "barButtonItem1";
            barButtonItem1.Id = 1;
            barButtonItem1.Name = "barButtonItem1";
            // 
            // btnRefresh
            // 
            btnRefresh.Caption = "Refresh";
            btnRefresh.Id = 3;
            btnRefresh.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("barButtonItem2.ImageOptions.Image");
            btnRefresh.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("barButtonItem2.ImageOptions.LargeImage");
            btnRefresh.ItemAppearance.Disabled.Options.UseFont = true;
            btnRefresh.ItemAppearance.Hovered.Font = new System.Drawing.Font("Tahoma", 12F);
            btnRefresh.ItemAppearance.Hovered.Options.UseFont = true;
            btnRefresh.ItemAppearance.Normal.Font = new System.Drawing.Font("Tahoma", 12F);
            btnRefresh.ItemAppearance.Normal.Options.UseFont = true;
            btnRefresh.ItemAppearance.Pressed.Font = new System.Drawing.Font("Tahoma", 12F);
            btnRefresh.ItemAppearance.Pressed.Options.UseFont = true;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.ItemClick += btnRefresh_ItemClick;
            // 
            // btnSendMsg
            // 
            btnSendMsg.Caption = "Message";
            btnSendMsg.Id = 5;
            btnSendMsg.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnSendMsg.ImageOptions.Image");
            btnSendMsg.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnSendMsg.ImageOptions.LargeImage");
            btnSendMsg.Name = "btnSendMsg";
            btnSendMsg.ItemClick += btnSendMsg_ItemClick;
            // 
            // barButtonItem3
            // 
            barButtonItem3.Caption = "barButtonItem3";
            barButtonItem3.Id = 6;
            barButtonItem3.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("barButtonItem3.ImageOptions.Image");
            barButtonItem3.Name = "barButtonItem3";
            // 
            // ribbonPageCategory1
            // 
            ribbonPageCategory1.Name = "ribbonPageCategory1";
            ribbonPageCategory1.Text = "ribbonPageCategory1";
            // 
            // ribbonPage1
            // 
            ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] { ribbonPageGroup1 });
            ribbonPage1.Name = "ribbonPage1";
            ribbonPage1.Text = "Info";
            // 
            // ribbonPageGroup1
            // 
            ribbonPageGroup1.ItemLinks.Add(btnRefresh);
            ribbonPageGroup1.ItemLinks.Add(btnSendMsg);
            ribbonPageGroup1.Name = "ribbonPageGroup1";
            // 
            // gridControl1
            // 
            gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            gridControl1.Location = new System.Drawing.Point(0, 146);
            gridControl1.MainView = gridView1;
            gridControl1.MenuManager = ribbonControl1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new System.Drawing.Size(1371, 666);
            gridControl1.TabIndex = 1;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            // 
            // Form1
            // 
            ActiveGlowColor = System.Drawing.Color.Black;
            Appearance.BackColor = System.Drawing.Color.Black;
            Appearance.ForeColor = System.Drawing.Color.Black;
            Appearance.Options.UseBackColor = true;
            Appearance.Options.UseFont = true;
            Appearance.Options.UseForeColor = true;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1371, 812);
            Controls.Add(gridControl1);
            Controls.Add(ribbonControl1);
            Font = new System.Drawing.Font("Segoe UI", 8.25F);
            Name = "Form1";
            Ribbon = ribbonControl1;
            Text = "PcInfo";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl1;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraBars.Ribbon.RibbonPageCategory ribbonPageCategory1;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraBars.BarButtonItem barButtonItem1;
        private DevExpress.XtraBars.BarButtonItem btnRefresh;
        private DevExpress.XtraBars.BarButtonItem btnSendMsg;
        private DevExpress.XtraBars.BarButtonItem barButtonItem3;
    }
}

