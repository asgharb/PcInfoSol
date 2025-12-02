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
            btnSend = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            txtIpFrom = new System.Windows.Forms.TextBox();
            txtIpTo = new System.Windows.Forms.TextBox();
            txtPassword = new System.Windows.Forms.TextBox();
            pnlText = new System.Windows.Forms.Panel();
            SuspendLayout();
            // 
            // txtMsg
            // 
            txtMsg.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtMsg.Location = new System.Drawing.Point(12, 12);
            txtMsg.Name = "txtMsg";
            txtMsg.Size = new System.Drawing.Size(711, 204);
            txtMsg.TabIndex = 0;
            txtMsg.Text = "";
            // 
            // btnSend
            // 
            btnSend.Font = new System.Drawing.Font("Segoe UI", 12F);
            btnSend.Location = new System.Drawing.Point(12, 343);
            btnSend.Name = "btnSend";
            btnSend.Size = new System.Drawing.Size(223, 39);
            btnSend.TabIndex = 3;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += BtnSend_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 12F);
            btnCancel.Location = new System.Drawing.Point(241, 343);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(239, 39);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtIpFrom
            // 
            txtIpFrom.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            txtIpFrom.Location = new System.Drawing.Point(12, 289);
            txtIpFrom.Name = "txtIpFrom";
            txtIpFrom.PlaceholderText = "Start Ip";
            txtIpFrom.Size = new System.Drawing.Size(223, 35);
            txtIpFrom.TabIndex = 1;
            txtIpFrom.KeyPress += txtIpFrom_KeyPress;
            // 
            // txtIpTo
            // 
            txtIpTo.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            txtIpTo.Location = new System.Drawing.Point(241, 289);
            txtIpTo.Name = "txtIpTo";
            txtIpTo.PlaceholderText = "End Ip";
            txtIpTo.Size = new System.Drawing.Size(239, 35);
            txtIpTo.TabIndex = 2;
            txtIpTo.KeyPress += txtIpFrom_KeyPress;
            // 
            // txtPassword
            // 
            txtPassword.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            txtPassword.Location = new System.Drawing.Point(12, 233);
            txtPassword.Name = "txtPassword";
            txtPassword.PlaceholderText = "Password";
            txtPassword.Size = new System.Drawing.Size(223, 35);
            txtPassword.TabIndex = 1;
            // 
            // pnlText
            // 
            pnlText.BackColor = System.Drawing.Color.White;
            pnlText.Location = new System.Drawing.Point(509, 289);
            pnlText.Name = "pnlText";
            pnlText.Size = new System.Drawing.Size(139, 90);
            pnlText.TabIndex = 5;
            pnlText.Visible = false;
            // 
            // FrmSendMsg
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(813, 401);
            Controls.Add(pnlText);
            Controls.Add(txtIpTo);
            Controls.Add(txtPassword);
            Controls.Add(txtIpFrom);
            Controls.Add(btnCancel);
            Controls.Add(btnSend);
            Controls.Add(txtMsg);
            Name = "FrmSendMsg";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "FrmSendMsg";
            Load += FrmSendMsg_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox txtMsg;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtIpFrom;
        private System.Windows.Forms.TextBox txtIpTo;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Panel pnlText;
    }
}