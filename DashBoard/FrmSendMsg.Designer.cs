namespace DashBoard
{
    partial class FrmSendMsg
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
            txtMsg = new System.Windows.Forms.RichTextBox();
            BtnSend = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            txtIpFrom = new System.Windows.Forms.TextBox();
            txtIpTo = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // txtMsg
            // 
            txtMsg.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtMsg.Location = new System.Drawing.Point(12, 12);
            txtMsg.Name = "txtMsg";
            txtMsg.Size = new System.Drawing.Size(628, 88);
            txtMsg.TabIndex = 0;
            txtMsg.Text = "";
            // 
            // BtnSend
            // 
            BtnSend.Location = new System.Drawing.Point(12, 174);
            BtnSend.Name = "BtnSend";
            BtnSend.Size = new System.Drawing.Size(159, 29);
            BtnSend.TabIndex = 3;
            BtnSend.Text = "Send";
            BtnSend.UseVisualStyleBackColor = true;
            BtnSend.Click += BtnSend_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(177, 174);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(156, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtIpFrom
            // 
            txtIpFrom.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            txtIpFrom.Location = new System.Drawing.Point(12, 122);
            txtIpFrom.Name = "txtIpFrom";
            txtIpFrom.Size = new System.Drawing.Size(159, 35);
            txtIpFrom.TabIndex = 1;
            txtIpFrom.KeyPress += txtIpFrom_KeyPress;
            // 
            // txtIpTo
            // 
            txtIpTo.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            txtIpTo.Location = new System.Drawing.Point(177, 122);
            txtIpTo.Name = "txtIpTo";
            txtIpTo.Size = new System.Drawing.Size(159, 35);
            txtIpTo.TabIndex = 2;
            txtIpTo.KeyPress += txtIpFrom_KeyPress;
            // 
            // FrmSendMsg
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(652, 215);
            Controls.Add(txtIpTo);
            Controls.Add(txtIpFrom);
            Controls.Add(btnCancel);
            Controls.Add(BtnSend);
            Controls.Add(txtMsg);
            Name = "FrmSendMsg";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "FrmSendMsg";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox txtMsg;
        private System.Windows.Forms.Button BtnSend;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtIpFrom;
        private System.Windows.Forms.TextBox txtIpTo;
    }
}