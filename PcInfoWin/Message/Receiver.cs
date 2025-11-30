using System;
using System.Drawing;
using System.Drawing.Text;
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


            string desiredFont = "Segoe UI";
            float fontSize = 12f;
            Font fontToUse;
            // گرفتن لیست فونت‌های نصب شده
            InstalledFontCollection installedFonts = new InstalledFontCollection();
            bool fontExists = installedFonts.Families.Any(f => f.Name.Equals(desiredFont, StringComparison.OrdinalIgnoreCase));

            if (fontExists)
            {
                fontToUse = new Font(desiredFont, fontSize, FontStyle.Regular);
            }
            else
            {
                fontToUse = new Font("Arial", fontSize, FontStyle.Regular);
            }

            // استفاده از فونت در Label
            Label lblMessage = new Label()
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = fontToUse
            };

            Button btnOk = new Button()
            {
                Text = "باشه",
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font(fontToUse.FontFamily, 12f, FontStyle.Regular),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) => this.Close();

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnOk);
        }
    }
}




//using System;
//using System.Drawing;
//using System.Drawing.Text;
//using System.Linq;
//using System.Net.NetworkInformation;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace PcInfoWin.Message
//{
//    public class Receiver
//    {
//        private readonly int port;
//        private UdpClient listener;

//        public Receiver(int port)
//        {
//            this.port = port;
//        }

//        public void StartListening()
//        {
//            if (IsPortInUse(port))
//            {
//                //MessageBox.Show($"پورت {port} در حال استفاده است و نمی‌توان به آن گوش داد.",
//                //    "خطا در باز کردن پورت", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return;
//            }

//            listener = new UdpClient(port);
//            Task.Run(async () =>
//            {
//                while (listener != null)
//                {
//                    try
//                    {
//                        var result = await listener.ReceiveAsync();
//                        // دریافت با UTF-8 و نرمال‌سازی
//                        string message = Encoding.UTF8.GetString(result.Buffer);
//                        message = message.Normalize(NormalizationForm.FormC);
//                        ShowMessage(message);
//                    }
//                    catch (ObjectDisposedException)
//                    {
//                        // listener بسته شده، حلقه را متوقف کن
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        MessageBox.Show("خطا در دریافت پیام: " + ex.Message);
//                        break;
//                    }
//                }
//            });
//        }

//        public void StopListening()
//        {
//            try
//            {
//                listener?.Close();
//                listener = null;
//            }
//            catch { }
//        }

//        private void ShowMessage(string message)
//        {
//            if (Application.MessageLoop)
//                ShowForm(message);
//            else
//                Application.Run(new MessageForm(message));
//        }

//        private void ShowForm(string message)
//        {
//            MessageForm msgForm = new MessageForm(message);
//            msgForm.TopMost = true;
//            msgForm.StartPosition = FormStartPosition.CenterScreen;
//            msgForm.Show();
//        }

//        // ✅ بررسی باز بودن پورت
//        private bool IsPortInUse(int port)
//        {
//            var ipProps = IPGlobalProperties.GetIPGlobalProperties();

//            // بررسی TCP Listenerها
//            var tcpListeners = ipProps.GetActiveTcpListeners();
//            if (tcpListeners.Any(p => p.Port == port))
//                return true;

//            // بررسی UDP Listenerها
//            var udpListeners = ipProps.GetActiveUdpListeners();
//            if (udpListeners.Any(p => p.Port == port))
//                return true;

//            return false;
//        }
//    }

//    public class MessageForm : Form
//    {
//        public MessageForm(string message)
//        {
//            this.Text = "پیام شبکه";
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.TopMost = true;
//            this.ClientSize = new Size(450, 250);
//            this.RightToLeft = RightToLeft.Yes;
//            this.RightToLeftLayout = true;

//            // انتخاب فونت مناسب
//            string desiredFont = "Tahoma"; // تغییر به Tahoma برای پشتیبانی بهتر از فارسی
//            float fontSize = 10f;
//            Font fontToUse;

//            // گرفتن لیست فونت‌های نصب شده
//            InstalledFontCollection installedFonts = new InstalledFontCollection();
//            bool fontExists = installedFonts.Families.Any(f => f.Name.Equals(desiredFont, StringComparison.OrdinalIgnoreCase));

//            if (fontExists)
//            {
//                fontToUse = new Font(desiredFont, fontSize, FontStyle.Regular);
//            }
//            else
//            {
//                // در صورت نبودن Tahoma از B Nazanin استفاده کن
//                bool nazaninExists = installedFonts.Families.Any(f => f.Name.Equals("B Nazanin", StringComparison.OrdinalIgnoreCase));
//                if (nazaninExists)
//                    fontToUse = new Font("B Nazanin", fontSize, FontStyle.Regular);
//                else
//                    fontToUse = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular);
//            }

//            // Panel برای نگه‌داشتن پیام
//            Panel pnlMessage = new Panel()
//            {
//                Dock = DockStyle.Fill,
//                Padding = new Padding(10),
//                BackColor = Color.White
//            };

//            // Label با تنظیمات کامل
//            Label lblMessage = new Label()
//            {
//                Text = message,
//                AutoSize = false,
//                TextAlign = ContentAlignment.MiddleRight, // راست‌چین برای فارسی
//                Dock = DockStyle.Fill,
//                Font = fontToUse,
//                RightToLeft = RightToLeft.Yes,
//                Padding = new Padding(5)
//            };

//            Button btnOk = new Button()
//            {
//                Text = "باشه",
//                Dock = DockStyle.Bottom,
//                Height = 45,
//                Font = new Font(fontToUse.FontFamily, 10f, FontStyle.Bold),
//                BackColor = Color.FromArgb(0, 120, 215),
//                ForeColor = Color.White,
//                FlatStyle = FlatStyle.Flat,
//                Cursor = Cursors.Hand
//            };
//            btnOk.FlatAppearance.BorderSize = 0;
//            btnOk.Click += (s, e) => this.Close();

//            pnlMessage.Controls.Add(lblMessage);
//            this.Controls.Add(pnlMessage);
//            this.Controls.Add(btnOk);
//        }
//    }
//}


//using System;
//using System.Drawing;
//using System.Drawing.Text;
//using System.Linq;
//using System.Net.NetworkInformation;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace PcInfoWin.Message
//{
//    public class Receiver
//    {
//        private readonly int port;
//        private UdpClient listener;

//        public Receiver(int port)
//        {
//            this.port = port;
//        }

//        public void StartListening()
//        {
//            if (IsPortInUse(port))
//            {
//                return;
//            }

//            listener = new UdpClient(port);
//            Task.Run(async () =>
//            {
//                while (listener != null)
//                {
//                    try
//                    {
//                        var result = await listener.ReceiveAsync();
//                        string message = Encoding.UTF8.GetString(result.Buffer);
//                        message = message.Normalize(NormalizationForm.FormC);
//                        ShowMessage(message);
//                    }
//                    catch (ObjectDisposedException)
//                    {
//                        break;
//                    }
//                    catch (Exception ex)
//                    {
//                        MessageBox.Show("خطا در دریافت پیام: " + ex.Message);
//                        break;
//                    }
//                }
//            });
//        }

//        public void StopListening()
//        {
//            try
//            {
//                listener?.Close();
//                listener = null;
//            }
//            catch { }
//        }

//        private void ShowMessage(string message)
//        {
//            if (Application.MessageLoop)
//                ShowForm(message);
//            else
//                Application.Run(new MessageForm(message));
//        }

//        private void ShowForm(string message)
//        {
//            MessageForm msgForm = new MessageForm(message);
//            msgForm.TopMost = true;
//            msgForm.StartPosition = FormStartPosition.CenterScreen;
//            msgForm.Show();
//        }

//        private bool IsPortInUse(int port)
//        {
//            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
//            var tcpListeners = ipProps.GetActiveTcpListeners();
//            if (tcpListeners.Any(p => p.Port == port))
//                return true;

//            var udpListeners = ipProps.GetActiveUdpListeners();
//            if (udpListeners.Any(p => p.Port == port))
//                return true;

//            return false;
//        }
//    }

//    public class MessageForm : Form
//    {
//        // کاراکترهای کنترل Unicode برای جهت‌دهی
//        private const char RLM = '\u200F';  // Right-to-Left Mark
//        private const char RLE = '\u202B';  // Right-to-Left Embedding
//        private const char PDF = '\u202C';  // Pop Directional Formatting

//        public MessageForm(string message)
//        {
//            this.Text = "پیام شبکه";
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.TopMost = true;
//            this.ClientSize = new Size(450, 250);
//            this.RightToLeft = RightToLeft.Yes;
//            this.RightToLeftLayout = true;
//            this.BackColor = Color.FromArgb(245, 245, 245);

//            Font fontToUse = GetBestPersianFont();

//            Panel pnlMessage = new Panel()
//            {
//                Dock = DockStyle.Fill,
//                Padding = new Padding(20),
//                BackColor = Color.White
//            };

//            // اضافه کردن کاراکترهای کنترل به متن برای راست‌چینی صحیح
//            string formattedMessage = FormatMessageRTL(message);

//            Label lblMessage = new Label()
//            {
//                Text = formattedMessage,
//                AutoSize = false,
//                Dock = DockStyle.Fill,
//                Font = fontToUse,
//                RightToLeft = RightToLeft.Yes,
//                TextAlign = ContentAlignment.TopRight,
//                Padding = new Padding(10),
//                ForeColor = Color.FromArgb(51, 51, 51),
//                UseCompatibleTextRendering = true // مهم
//            };

//            Button btnOk = new Button()
//            {
//                Text = "باشه",
//                Dock = DockStyle.Bottom,
//                Height = 50,
//                Font = new Font(fontToUse.FontFamily, 12f, FontStyle.Regular),
//                BackColor = Color.FromArgb(0, 120, 215),
//                ForeColor = Color.White,
//                FlatStyle = FlatStyle.Flat,
//                Cursor = Cursors.Hand
//            };
//            btnOk.FlatAppearance.BorderSize = 0;
//            btnOk.Click += (s, e) => this.Close();

//            pnlMessage.Controls.Add(lblMessage);
//            this.Controls.Add(pnlMessage);
//            this.Controls.Add(btnOk);
//        }

//        private string FormatMessageRTL(string message)
//        {
//            const char RLE = '\u202B'; // شروع بلاک راست‌به‌چپ
//            const char PDF = '\u202C'; // پایان بلاک

//            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
//            StringBuilder sb = new StringBuilder();

//            foreach (var line in lines)
//            {
//                // کل خط داخل بلاک RTL
//                sb.Append(RLE);
//                sb.Append(line);
//                sb.Append(PDF);
//                sb.AppendLine();
//            }

//            return sb.ToString();
//        }

//        private Font GetBestPersianFont()
//        {
//            InstalledFontCollection installedFonts = new InstalledFontCollection();

//            string[] persianFonts = new string[]
//            {
//                "Vazir",
//                "IRANSans",
//                "B Nazanin",
//                "Nazanin",
//                "Tahoma"
//            };

//            foreach (string fontName in persianFonts)
//            {
//                bool fontExists = installedFonts.Families.Any(f =>
//                    f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase));

//                if (fontExists)
//                {
//                    return new Font(fontName, 13f, FontStyle.Regular);
//                }
//            }

//            return new Font("Tahoma", 13f, FontStyle.Regular);
//        }
//    }
//}
