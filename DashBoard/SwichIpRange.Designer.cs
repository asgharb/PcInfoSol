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
            txtFrom.Location = new System.Drawing.Point(12, 12);
            txtFrom.Name = "txtFrom";
            txtFrom.Size = new System.Drawing.Size(148, 23);
            txtFrom.TabIndex = 0;
            txtFrom.KeyPress += txtFrom_KeyPress;
            // 
            // txtTo
            // 
            txtTo.Location = new System.Drawing.Point(166, 12);
            txtTo.Name = "txtTo";
            txtTo.Size = new System.Drawing.Size(148, 23);
            txtTo.TabIndex = 0;
            txtTo.KeyPress += txtTo_KeyPress;
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(12, 53);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(148, 23);
            btnOk.TabIndex = 1;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(166, 53);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(148, 23);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // SwichIpRange
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(335, 94);
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