namespace DashBoard
{
    partial class SwichIpRange
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
            txtFrom = new System.Windows.Forms.TextBox();
            txtTo = new System.Windows.Forms.TextBox();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // txtFrom
            // 
            txtFrom.Font = new System.Drawing.Font("Segoe UI", 12F);
            txtFrom.Location = new System.Drawing.Point(12, 12);
            txtFrom.Name = "txtFrom";
            txtFrom.Size = new System.Drawing.Size(187, 29);
            txtFrom.TabIndex = 0;
            txtFrom.KeyPress += txtFrom_KeyPress;
            // 
            // txtTo
            // 
            txtTo.Font = new System.Drawing.Font("Segoe UI", 12F);
            txtTo.Location = new System.Drawing.Point(205, 12);
            txtTo.Name = "txtTo";
            txtTo.Size = new System.Drawing.Size(185, 29);
            txtTo.TabIndex = 1;
            txtTo.KeyPress += txtTo_KeyPress;
            // 
            // btnOk
            // 
            btnOk.Font = new System.Drawing.Font("Segoe UI", 12F);
            btnOk.Location = new System.Drawing.Point(12, 74);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(187, 32);
            btnOk.TabIndex = 2;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 12F);
            btnCancel.Location = new System.Drawing.Point(205, 74);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(185, 32);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // SwichIpRange
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(397, 118);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(txtTo);
            Controls.Add(txtFrom);
            Name = "SwichIpRange";
            Text = "SwichIpRange";
            Load += SwichIpRange_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtFrom;
        private System.Windows.Forms.TextBox txtTo;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}