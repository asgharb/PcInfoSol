using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DashBoard
{
    public partial class FrmSendMsg : Form
    {
        private UdpClient listener;
        private CancellationTokenSource cts;
        private const int Port = 9000;

        public FrmSendMsg()
        {
            InitializeComponent();
        }


        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string msg = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg))
            {
                MessageBox.Show("متن پیام را وارد کنید.");
                return;
            }

            string ipFrom = txtIpFrom.Text.Trim();
            string ipTo = txtIpTo.Text.Trim();

            try
            {
                await SendMessageAsync(msg, ipFrom, ipTo);
                MessageBox.Show("✅ پیام ارسال شد.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ خطا در ارسال: " + ex.Message);
            }
        }



        private async Task ListenForMessages(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await listener.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    // نمایش بالون در ترد UI
                    this.Invoke((MethodInvoker)delegate
                    {
                        NotifyIcon notify = new NotifyIcon();
                        notify.Visible = true;
                        notify.Icon = SystemIcons.Information;
                        notify.BalloonTipTitle = "📨 پیام جدید";
                        notify.BalloonTipText = message;
                        notify.ShowBalloonTip(5000);
                    });
                }
            }
            catch (ObjectDisposedException)
            {
                // نادیده بگیر (زمان توقف)
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                cts?.Cancel();
                listener?.Close();
                listener = null;
                MessageBox.Show("گوش دادن متوقف شد.");
            }
            catch { }
        }



        //private async Task SendMessageAsync(string message, string ipFrom, string ipTo)
        //{
        //    try
        //    {
        //        byte[] data = Encoding.UTF8.GetBytes(message);

        //        using (UdpClient client = new UdpClient())
        //        {
        //            client.EnableBroadcast = true;

        //            // حالت ۵ → ارسال به همه (Broadcast)
        //            if (ipFrom.Equals("all", StringComparison.OrdinalIgnoreCase))
        //            {
        //                IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, Port);
        //                await client.SendAsync(data, data.Length, broadcast);
        //                return;
        //            }

        //            // تبدیل IP‌ها به عدد برای محدوده
        //            uint from = IpToUint(ipFrom);
        //            uint to = IpToUint(ipTo);

        //            if (from > to)
        //            {
        //                uint temp = from;
        //                from = to;
        //                to = temp;
        //            }

        //            // ارسال به همه IPها در محدوده
        //            for (uint ip = from; ip <= to; ip++)
        //            {
        //                string ipStr = UintToIp(ip);
        //                try
        //                {
        //                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipStr), Port);
        //                    await client.SendAsync(data, data.Length, endPoint);
        //                }
        //                catch
        //                {
        //                    // خطا در یک IP → ادامه بده
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("خطا در ارسال پیام: " + ex.Message);
        //    }
        //}

        private async Task SendMessageAsync(string message, string ipFrom, string ipTo)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);

                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true;

                    // حالت broadcast به همه
                    if (ipFrom.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, Port);
                        await client.SendAsync(data, data.Length, broadcast);
                        return;
                    }

                    string[] fromParts = ipFrom.Split('.');
                    string[] toParts = ipTo.Split('.');

                    int fromA = int.Parse(fromParts[0]);
                    int toA = int.Parse(toParts[0]);
                    int fromB = int.Parse(fromParts[1]);
                    int toB = int.Parse(toParts[1]);
                    int fromC = int.Parse(fromParts[2]);
                    int toC = int.Parse(toParts[2]);
                    int fromD = int.Parse(fromParts[3]);
                    int toD = int.Parse(toParts[3]);

                    for (int a = fromA; a <= toA; a++)
                    {
                        for (int b = (a == fromA ? fromB : 0); b <= (a == toA ? toB : 255); b++)
                        {
                            for (int c = (a == fromA && b == fromB ? fromC : 0);
                                 c <= (a == toA && b == toB ? toC : 255); c++)
                            {
                                for (int d = (a == fromA && b == fromB && c == fromC ? fromD : 0);
                                     d <= (a == toA && b == toB && c == toC ? toD : 255); d++)
                                {
                                    string ipStr = $"{a}.{b}.{c}.{d}";
                                    try
                                    {
                                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipStr), Port);
                                        await client.SendAsync(data, data.Length, endPoint);
                                        await Task.Delay(2); // تأخیر کوتاه برای جلوگیری از ازدحام
                                    }
                                    catch { /* ادامه بده */ }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("خطا در ارسال پیام: " + ex.Message);
            }
        }



        private uint IpToUint(string ip)
        {
            string[] parts = ip.Split('.');
            if (parts.Length != 4) throw new Exception("فرمت IP نادرست است.");
            return (uint)(int.Parse(parts[0]) << 24 |
                          int.Parse(parts[1]) << 16 |
                          int.Parse(parts[2]) << 8 |
                          int.Parse(parts[3]));
        }

        private string UintToIp(uint ip)
        {
            return string.Join(".",
                (ip >> 24) & 0xFF,
                (ip >> 16) & 0xFF,
                (ip >> 8) & 0xFF,
                ip & 0xFF);
        }

        private void txtIpFrom_KeyPress(object sender, KeyPressEventArgs e)
        {
            // فقط عدد، نقطه، و دکمه‌های کنترل مثل بک‌اسپیس مجازه
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // جلوگیری از وارد کردن دو نقطه پشت سر هم
            TextBox txt = sender as TextBox;
            if (e.KeyChar == '.' && txt != null)
            {
                if (txt.Text.EndsWith("."))
                    e.Handled = true;
            }
        }
    }
}

