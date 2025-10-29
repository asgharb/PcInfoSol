using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcInfoWin.Message
{
    public class Receiver
    {
        private readonly int port;
        private UdpClient listener;

        public Receiver(int port)
        {
            this.port = port;
        }

        public void StartListening()
        {
            listener = new UdpClient(port);
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var result = await listener.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);
                        ShowMessage(message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطا در دریافت پیام: " + ex.Message);
                    }
                }
            });
        }

        private void ShowMessage(string message)
        {
            // چون این تابع در ترد غیر UI صدا زده می‌شود، باید در ترد UI اجرا شود
            if (Application.MessageLoop)
                ShowForm(message);
            else
                Application.Run(new MessageForm(message));
        }

        private void ShowForm(string message)
        {
            MessageForm msgForm = new MessageForm(message);
            msgForm.TopMost = true;
            msgForm.StartPosition = FormStartPosition.CenterScreen;
            msgForm.Show();
        }
    }

    public class MessageForm : Form
    {
        public MessageForm(string message)
        {
            this.Text = "پیام شبکه";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.ClientSize = new Size(400, 200);

            Label lblMessage = new Label()
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Regular)
            };

            Button btnOk = new Button()
            {
                Text = "باشه",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            btnOk.Click += (s, e) => this.Close();

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnOk);
        }
    }
}

