using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DashBoard
{
    public static class IpRangeHelper
    {
        // تبدیل IPv4 string => uint (big-endian)
        private static uint IPv4ToUint(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length != 4) throw new ArgumentException("آی‌پی نامعتبر: " + ip);
            return (uint)(int.Parse(parts[0]) << 24 | int.Parse(parts[1]) << 16 | int.Parse(parts[2]) << 8 | int.Parse(parts[3]));
        }

        // تبدیل uint => IPv4 string
        private static string UintToIPv4(uint value)
        {
            return string.Format("{0}.{1}.{2}.{3}",
                (value >> 24) & 0xFF,
                (value >> 16) & 0xFF,
                (value >> 8) & 0xFF,
                value & 0xFF);
        }

        // بازهٔ IP (شامل start و end). ترتیب معکوس هم پشتیبانی می شود.
        public static IEnumerable<string> GetIPsInRange(string startIp, string endIp)
        {
            uint s = IPv4ToUint(startIp);
            uint e = IPv4ToUint(endIp);

            if (s <= e)
            {
                for (uint cur = s; cur <= e; cur++)
                {
                    yield return UintToIPv4(cur);
                    if (cur == UInt32.MaxValue) break; // جلوگیری از overflow
                }
            }
            else
            {
                for (uint cur = s; cur >= e; cur--)
                {
                    yield return UintToIPv4(cur);
                    if (cur == 0) break; // جلوگیری از بی‌نهایت حلقه
                }
            }
        }

        // ارسال پیام به یک IP
        public static async Task SendUdpToAsync(string ipAddress, int port, string message)
        {
            using (UdpClient client = new UdpClient())
            {
                // فقط وقتی میخوای broadcast کنی، EnableBroadcast = true بذار
                if (ipAddress == "255.255.255.255") client.EnableBroadcast = true;

                var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(bytes, bytes.Length, endPoint);
            }
        }

        // ارسال پیام به مجموعه آدرس‌ها — با یک تاخیر کوتاه بین ارسال‌ها برای جلوگیری از flooding شبکه
        public static async Task SendToManyAsync(IEnumerable<string> ips, int port, string message, int delayMs = 5)
        {
            foreach (var ip in ips)
            {
                try
                {
                    await SendUdpToAsync(ip, port, message);
                }
                catch
                {
                    // اگر خواستی لاگ بگیر یا خطا نشان بده
                }

                if (delayMs > 0)
                    await Task.Delay(delayMs);
            }
        }
    }
}
