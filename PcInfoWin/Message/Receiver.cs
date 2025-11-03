using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
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
            if (IsPortInUse(port))
            {
                //MessageBox.Show($"پورت {port} در حال استفاده است و نمی‌توان به آن گوش داد.",
                //    "خطا در باز کردن پورت", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            listener = new UdpClient(port);
            Task.Run(async () =>
            {
                while (listener != null)
                {
                    try
                    {
                        var result = await listener.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);
                        ShowMessage(message);
                    }
                    catch (ObjectDisposedException)
                    {
                        // listener بسته شده، حلقه را متوقف کن
                        break;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطا در دریافت پیام: " + ex.Message);
                        break;
                    }
                }
            });
        }

        public void StopListening()
        {
            try
            {
                listener?.Close();
                listener = null;
            }
            catch { }
        }

        private void ShowMessage(string message)
        {
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

        // ✅ بررسی باز بودن پورت
        private bool IsPortInUse(int port)
        {
            var ipProps = IPGlobalProperties.GetIPGlobalProperties();

            // بررسی TCP Listenerها
            var tcpListeners = ipProps.GetActiveTcpListeners();
            if (tcpListeners.Any(p => p.Port == port))
                return true;

            // بررسی UDP Listenerها
            var udpListeners = ipProps.GetActiveUdpListeners();
            if (udpListeners.Any(p => p.Port == port))
                return true;

            return false;
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
