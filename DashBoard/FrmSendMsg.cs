//using System;
//using System.Drawing;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace DashBoard
//{
//    public partial class FrmSendMsg : Form
//    {
//        private UdpClient listener;
//        //private CancellationTokenSource cts;
//        private const int Port = 9000;

//        public FrmSendMsg()
//        {
//            InitializeComponent();
//        }
//        private void FrmSendMsg_Load(object sender, EventArgs e)
//        {
//            txtMsg.RightToLeft = RightToLeft.Yes;
//        }
//        private void btnCancel_Click(object sender, EventArgs e)
//        {
//            this.Close();
//        }

//        private async void BtnSend_Click(object sender, EventArgs e)
//        {
//            string msg = txtMsg.Text?.Trim();
//            if (string.IsNullOrEmpty(msg))
//            {
//                MessageBox.Show("متن پیام را وارد کنید.");
//                return;
//            }

//            this.Cursor = Cursors.WaitCursor;
//            // اضافه کردن نام سیستم به صورت ایمن
//            string machineName = string.IsNullOrWhiteSpace(Environment.MachineName)
//                ? "Unknown"
//                : Environment.MachineName;

//            // حذف "-pc" در صورتی که آخر رشته باشد (غیرحساس به حروف)
//            if (machineName.EndsWith("-pc", StringComparison.OrdinalIgnoreCase))
//            {
//                machineName = machineName.Substring(0, machineName.Length - 3);
//            }

//            msg += "\n\n" + machineName;


//            string ipFrom = txtIpFrom.Text.Trim();
//            string ipTo = txtIpTo.Text.Trim();

//            try
//            {
//                await SendMessageAsync(msg, ipFrom, ipTo);
//                MessageBox.Show("✅ پیام ارسال شد.");
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("❌ خطا در ارسال: " + ex.Message);
//            }
//            finally
//            {
//                this.Cursor = Cursors.Default;
//            }
//        }

//        private async Task SendMessageAsync(string message, string ipFrom, string ipTo)
//        {
//            try
//            {
//                byte[] data = Encoding.UTF8.GetBytes(message);

//                using (UdpClient client = new UdpClient())
//                {
//                    client.EnableBroadcast = true;

//                    // حالت broadcast به همه
//                    if (ipFrom.Equals("all", StringComparison.OrdinalIgnoreCase))
//                    {
//                        IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, Port);
//                        await client.SendAsync(data, data.Length, broadcast);
//                        return;
//                    }

//                    string[] fromParts = ipFrom.Split('.');
//                    string[] toParts = ipTo.Split('.');

//                    int fromA = int.Parse(fromParts[0]);
//                    int toA = int.Parse(toParts[0]);
//                    int fromB = int.Parse(fromParts[1]);
//                    int toB = int.Parse(toParts[1]);
//                    int fromC = int.Parse(fromParts[2]);
//                    int toC = int.Parse(toParts[2]);
//                    int fromD = int.Parse(fromParts[3]);
//                    int toD = int.Parse(toParts[3]);

//                    for (int a = fromA; a <= toA; a++)
//                    {
//                        for (int b = (a == fromA ? fromB : 0); b <= (a == toA ? toB : 255); b++)
//                        {
//                            for (int c = (a == fromA && b == fromB ? fromC : 0);
//                                 c <= (a == toA && b == toB ? toC : 255); c++)
//                            {
//                                for (int d = (a == fromA && b == fromB && c == fromC ? fromD : 0);
//                                     d <= (a == toA && b == toB && c == toC ? toD : 255); d++)
//                                {
//                                    string ipStr = $"{a}.{b}.{c}.{d}";
//                                    try
//                                    {
//                                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipStr), Port);
//                                        await client.SendAsync(data, data.Length, endPoint);
//                                        await Task.Delay(2); // تأخیر کوتاه برای جلوگیری از ازدحام
//                                    }
//                                    catch { /* ادامه بده */ }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("خطا در ارسال پیام: " + ex.Message);
//            }
//        }

//        private void txtIpFrom_KeyPress(object sender, KeyPressEventArgs e)
//        {
//            // فقط عدد، نقطه، و دکمه‌های کنترل مثل بک‌اسپیس مجازه
//            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
//            {
//                e.Handled = true;
//            }

//            // جلوگیری از وارد کردن دو نقطه پشت سر هم
//            TextBox txt = sender as TextBox;
//            if (e.KeyChar == '.' && txt != null)
//            {
//                if (txt.Text.EndsWith("."))
//                    e.Handled = true;
//            }
//        }
//    }
//}

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
        //private CancellationTokenSource cts;
        private const int Port = 9000;

        public FrmSendMsg()
        {
            InitializeComponent();
        }

        private void FrmSendMsg_Load(object sender, EventArgs e)
        {
            // تنظیمات TextBox برای پشتیبانی بهتر از فارسی
            txtMsg.RightToLeft = RightToLeft.Yes;
            txtMsg.Font = new Font("Tahoma", 14F);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {

            string msg = txtMsg.Text?.Trim();
            if (string.IsNullOrEmpty(msg))
            {
                MessageBox.Show("متن پیام را وارد کنید.");
                this.Cursor = Cursors.Default;
                return;
            }

            // نرمال‌سازی متن برای جلوگیری از مشکلات encoding
            msg = msg.Normalize(NormalizationForm.FormC);

            this.Cursor = Cursors.WaitCursor;

            // اضافه کردن نام سیستم به صورت ایمن
            string machineName = string.IsNullOrWhiteSpace(Environment.MachineName)
                ? "Unknown"
                : Environment.MachineName;

            // حذف "-pc" در صورتی که آخر رشته باشد (غیرحساس به حروف)
            if (machineName.EndsWith("-pc", StringComparison.OrdinalIgnoreCase))
            {
                machineName = machineName.Substring(0, machineName.Length - 3);
            }

            // استفاده از RLM (Right-to-Left Mark) برای جهت‌دهی صحیح متن
            const char RLM = '\u200F';
            msg = $"{msg}\n\n{RLM}";
            msg += $"{machineName} : ارسال از";

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
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async Task SendMessageAsync(string message, string ipFrom, string ipTo)
        {
            try
            {
                // استفاده از UTF-8 encoding با BOM برای پشتیبانی بهتر
                byte[] data = new UTF8Encoding(true).GetBytes(message);

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

                    // اعتبارسنجی ساده
                    if (fromParts.Length != 4 || toParts.Length != 4)
                    {
                        throw new Exception("فرمت IP نامعتبر است.");
                    }

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
                                    catch
                                    {
                                        // ادامه بده در صورت خطا در ارسال به یک IP خاص
                                    }
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


