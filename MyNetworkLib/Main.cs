using Renci.SshNet;
using SqlDataExtention.Entity;
using System.Text;
using System.Text.RegularExpressions;

namespace MyNetworkLib
{
    internal class RawMacEntry
    {
        public string Vlan { get; set; }
        public string Mac { get; set; }
        public string Port { get; set; }
    }

    public static class NetworkMapper
    {
        private const string SwitchUser = "infosw";
        private const string SwitchPass = "Ii123456!";
        private const int SshTimeoutSec = 10;

        /// <summary>
        /// متد اصلی: اسکن رنج آی‌پی و بازگرداندن لیست نهایی برای ذخیره در دیتابیس
        /// </summary>
        public static List<SwithInfo> ScanNetworkRange(string startIp, string endIp, IProgress<int> progress = null)
        {
            var results = new List<SwithInfo>();
            var ipList = GetIpRange(startIp, endIp);
            int counter = 0;

            foreach (var ip in ipList)
            {
                try
                {
                    var switchResults = ProcessSingleSwitch(ip);
                    results.AddRange(switchResults);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning {ip}: {ex.Message}");
                }

                counter++;
                progress?.Report(counter);
            }

            return results;
        }

        // ============================================================
        // منطق اصلی پردازش هر سوییچ
        // ============================================================
        //private static List<SwithInfo> ProcessSingleSwitch(string ip)
        //{
        //    var finalInfos = new List<SwithInfo>();

        //    using var client = new SshClient(ip, SwitchUser, SwitchPass);
        //    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(SshTimeoutSec);
        //    client.Connect();

        //    if (!client.IsConnected) return finalInfos;

        //    using var stream = client.CreateShellStream("dumb", 80, 24, 800, 600, 1024);

        //    // 0. آماده‌سازی ترمینال
        //    SendCommand(stream, "terminal length 0");

        //    // 1. دریافت لیست پورت‌های ترانک
        //    var trunkPorts = GetTrunkPorts(stream);

        //    // 2. دریافت جدول مک آدرس
        //    var macEntries = GetMacAddressTable(stream);

        //    // 3. دریافت اطلاعات CDP برای پیدا کردن IPها
        //    // نکته: حتما باید detail باشد تا IP را نشان دهد
        //    string cdpOutput = SendCommand(stream, "show cdp neighbors detail");

        //    client.Disconnect();

        //    // 4. گروه‌بندی بر اساس پورت برای تشخیص اینکه روی هر پورت چه دستگاه‌هایی است
        //    var groupedByPort = macEntries.GroupBy(x => x.Port);

        //    foreach (var group in groupedByPort)
        //    {
        //        var portName = group.Key;
        //        var entries = group.ToList();

        //        // بررسی اینکه آیا پورت ترانک است یا خیر
        //        bool isTrunk = trunkPorts.Contains(portName);

        //        // متغیرهای نگهدارنده
        //        RawMacEntry pcEntry = null;
        //        RawMacEntry phoneEntry = null;
        //        RawMacEntry vtEntry = null;

        //        foreach (var entry in entries)
        //        {
        //            // *** اعمال قوانین ۴ گانه شما ***

        //            if (isTrunk)
        //            {
        //                // قانون 3 و 4: اگر ترانک است
        //                if (entry.Vlan == "200")
        //                {
        //                    // قانون 4: ترانک و Vlan 200 -> VT
        //                    vtEntry = entry;
        //                }
        //                else
        //                {
        //                    // قانون 3: ترانک و Vlan غیر 200 -> نادیده بگیر
        //                    continue;
        //                }
        //            }
        //            else
        //            {
        //                // قانون 1 و 2: اگر ترانک نیست (اکسس است)
        //                if (entry.Vlan == "30")
        //                {
        //                    // قانون 1: اکسس و Vlan 30 -> تلفن
        //                    phoneEntry = entry;
        //                }
        //                else
        //                {
        //                    // قانون 2: اکسس و غیر Vlan 30 -> کامپیوتر (PC)
        //                    pcEntry = entry;
        //                }
        //            }
        //        }

        //        // اگر هیچ چیزی روی پورت پیدا نشد، رد شو
        //        if (pcEntry == null && phoneEntry == null && vtEntry == null) continue;

        //        // تلاش برای پیدا کردن IP از طریق CDP با متد اصلاح شده
        //        string deviceIp = ParseCdpIp(cdpOutput, portName);

        //        finalInfos.Add(new SwithInfo
        //        {
        //            SwitchIp = ip,
        //            SwitchPort = portName,

        //            // PC Info
        //            PcMac = FormatMac(pcEntry?.Mac),
        //            PcVlan = pcEntry?.Vlan,
        //            PcIp = null, // معمولا PC در CDP دیده نمی‌شود

        //            // Phone Info
        //            PhoneMac = FormatMac(phoneEntry?.Mac),
        //            PhoneVlan = phoneEntry?.Vlan,
        //            // اگر VT نباشد و Phone باشد، IP پیدا شده متعلق به تلفن است
        //            PhoneIp = (vtEntry == null && phoneEntry != null) ? deviceIp : null,

        //            // VT Info
        //            VTMac = FormatMac(vtEntry?.Mac),
        //            VTVlan = vtEntry?.Vlan,
        //            // اگر VT باشد، IP متعلق به VT است
        //            VTIP = (vtEntry != null) ? deviceIp : null,

        //            UserFullName = "" // بعدا پر می‌شود
        //        });
        //    }

        //    return finalInfos;
        //}

        private static List<SwithInfo> ProcessSingleSwitch(string ip)
        {
            var finalInfos = new List<SwithInfo>();

            // ... (کدهای اتصال SSH و دریافت اطلاعات مثل قبل) ...
            using var client = new SshClient(ip, SwitchUser, SwitchPass);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(SshTimeoutSec);
            client.Connect();

            if (!client.IsConnected) return finalInfos;

            using var stream = client.CreateShellStream("dumb", 80, 24, 800, 600, 1024);
            SendCommand(stream, "terminal length 0");
            var trunkPorts = GetTrunkPorts(stream);
            var macEntries = GetMacAddressTable(stream);
            string cdpOutput = SendCommand(stream, "show cdp neighbors detail");
            client.Disconnect();
            // ... (تا اینجا مثل قبل) ...

            var groupedByPort = macEntries.GroupBy(x => x.Port);

            foreach (var group in groupedByPort)
            {
                var portName = group.Key;
                var entries = group.ToList();
                bool isTrunk = trunkPorts.Contains(portName);

                RawMacEntry pcEntry = null;
                RawMacEntry phoneEntry = null;
                RawMacEntry vtEntry = null;

                foreach (var entry in entries)
                {
                    if (isTrunk)
                    {
                        // پورت‌های ترانک (VT)
                        if (entry.Vlan == "200") vtEntry = entry;
                    }
                    else
                    {
                        // پورت‌های اکسس (PC و Phone)
                        if (entry.Vlan == "30")
                        {
                            phoneEntry = entry;
                        }
                        else
                        {
                            // هر چیزی غیر از 30 را فعلا PC فرض می‌کنیم
                            pcEntry = entry;
                        }
                    }
                }

                // *** اصلاحیه مهم: حذف تکرار مک تلفن در جایگاه PC ***
                // اگر هم PC پیدا کردیم هم تلفن، و مک آدرس‌هایشان یکی بود
                // یعنی این entry که فکر میکردیم PC است، در واقع سایه تلفن در Vlan 1 بوده.
                if (pcEntry != null && phoneEntry != null)
                {
                    if (pcEntry.Mac == phoneEntry.Mac)
                    {
                        pcEntry = null; // این PC واقعی نیست، خود تلفن است
                    }
                }
                // ***************************************************

                if (pcEntry == null && phoneEntry == null && vtEntry == null) continue;

                string deviceIp = ParseCdpIp(cdpOutput, portName);

                finalInfos.Add(new SwithInfo
                {
                    SwitchIp = ip,
                    SwitchPort = portName,

                    PcMac = FormatMac(pcEntry?.Mac),
                    PcVlan = pcEntry?.Vlan,
                    PcIp = null,

                    PhoneMac = FormatMac(phoneEntry?.Mac),
                    PhoneVlan = phoneEntry?.Vlan,
                    PhoneIp = (vtEntry == null && phoneEntry != null) ? deviceIp : null,

                    VTMac = FormatMac(vtEntry?.Mac),
                    VTVlan = vtEntry?.Vlan,
                    VTIP = (vtEntry != null) ? deviceIp : null,

                    UserFullName = ""
                });
            }

            return finalInfos;
        }

        // ============================================================
        // متدهای کمکی SSH و Parse
        // ============================================================

        private static HashSet<string> GetTrunkPorts(ShellStream stream)
        {
            var trunks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string output = SendCommand(stream, "show interfaces trunk");

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(Gi|Fa|Te|GigabitEthernet|FastEthernet)\S+", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    trunks.Add(match.Value);
                }
            }
            return trunks;
        }

        private static List<RawMacEntry> GetMacAddressTable(ShellStream stream)
        {
            var list = new List<RawMacEntry>();
            string output = SendCommand(stream, "show mac address-table");

            var regex = new Regex(@"^\s*(?<vlan>\d+)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\w+\s+(?<port>\S+)", RegexOptions.Multiline);

            foreach (Match m in regex.Matches(output))
            {
                if (m.Groups["port"].Value.Contains("CPU", StringComparison.OrdinalIgnoreCase)) continue;

                list.Add(new RawMacEntry
                {
                    Vlan = m.Groups["vlan"].Value,
                    Mac = m.Groups["mac"].Value,
                    Port = m.Groups["port"].Value
                });
            }
            return list;
        }

        private static string SendCommand(ShellStream stream, string command)
        {
            stream.WriteLine(command);
            Thread.Sleep(200);

            StringBuilder output = new StringBuilder();
            var buffer = new byte[4096];

            while (stream.DataAvailable)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                output.Append(Encoding.UTF8.GetString(buffer, 0, read));
                Thread.Sleep(50);
            }

            return output.ToString();
        }

        /// <summary>
        /// متد اصلاح شده برای یافتن IP از خروجی CDP
        /// </summary>
        private static string ParseCdpIp(string cdpFullOutput, string portName)
        {
            if (string.IsNullOrEmpty(cdpFullOutput) || string.IsNullOrEmpty(portName)) return null;

            // 1. استخراج فقط شماره پورت از نام پورت ورودی (مثلا Gi1/0/22 -> 1/0/22)
            string targetPortNum = GetPortNumber(portName);
            if (string.IsNullOrEmpty(targetPortNum)) return null;

            // 2. جدا کردن بلوک‌های CDP
            var blocks = cdpFullOutput.Split(new[] { "-------------------------" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                // 3. پیدا کردن خط Interface در هر بلوک CDP
                // مثال: Interface: GigabitEthernet1/0/22,  Port ID (outgoing port): ...
                var interfaceMatch = Regex.Match(block, @"Interface:\s*([a-zA-Z]+)([\d\/]+)", RegexOptions.IgnoreCase);

                if (interfaceMatch.Success)
                {
                    // شماره پورتی که در خروجی CDP پیدا شد
                    string foundPortNum = interfaceMatch.Groups[2].Value;

                    // 4. مقایسه شماره پورت‌ها (فقط عدد و اسلش مقایسه می‌شود)
                    if (foundPortNum == targetPortNum)
                    {
                        // اگر پورت درست بود، دنبال IP بگرد
                        var ipMatch = Regex.Match(block, @"IP address:\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.IgnoreCase);
                        if (ipMatch.Success)
                        {
                            return ipMatch.Groups[1].Value;
                        }
                    }
                }
            }
            return null;
        }

        // تابع کمکی جدید برای حذف حروف و نگه‌داشتن فقط اعداد و اسلش
        private static string GetPortNumber(string rawPortName)
        {
            // Gi1/0/22 => 1/0/22
            // GigabitEthernet1/0/22 => 1/0/22
            return Regex.Replace(rawPortName, @"[^\d\/]", "");
        }

        // ============================================================
        // ابزارهای کمکی IP و فرمت‌بندی
        // ============================================================

        public static List<string> GetIpRange(string startIp, string endIp)
        {
            uint start = IpToUint(startIp);
            uint end = IpToUint(endIp);
            var list = new List<string>();
            for (uint ip = start; ip <= end; ip++) list.Add(UintToIp(ip));
            return list;
        }

        public static uint IpToUint(string ip)
        {
            var segments = ip.Split('.').Select(byte.Parse).ToArray();
            return (uint)(segments[0] << 24 | segments[1] << 16 | segments[2] << 8 | segments[3]);
        }

        public static string UintToIp(uint value)
        {
            return $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
        }

        private static string FormatMac(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            var clean = Regex.Replace(raw, "[^0-9a-fA-F]", "").ToUpper();
            if (clean.Length != 12) return raw;
            return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
        }
    }
}









//using Renci.SshNet;
//using SqlDataExtention.Entity;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    // کلاس کمکی داخلی برای نگهداری موقت اطلاعات خام
//    internal class RawMacEntry
//    {
//        public string Vlan { get; set; }
//        public string Mac { get; set; }
//        public string Port { get; set; }
//    }

//    public static class NetworkMapper
//    {
//        private const string SwitchUser = "infosw";
//        private const string SwitchPass = "Ii123456!";
//        private const int SshTimeoutSec = 10;

//        /// <summary>
//        /// متد اصلی: اسکن رنج آی‌پی و بازگرداندن لیست نهایی برای ذخیره در دیتابیس
//        /// </summary>
//        public static List<SwithInfo> ScanNetworkRange(string startIp, string endIp, IProgress<int> progress = null)
//        {
//            var results = new List<SwithInfo>();
//            var ipList = GetIpRange(startIp, endIp);
//            int counter = 0;

//            foreach (var ip in ipList)
//            {
//                try
//                {
//                    // اسکن هر سوییچ
//                    var switchResults = ProcessSingleSwitch(ip);
//                    results.AddRange(switchResults);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Error scanning {ip}: {ex.Message}");
//                }

//                counter++;
//                progress?.Report(counter);
//            }

//            return results;
//        }

//        // ============================================================
//        // منطق اصلی پردازش هر سوییچ
//        // ============================================================
//        private static List<SwithInfo> ProcessSingleSwitch(string ip)
//        {
//            var finalInfos = new List<SwithInfo>();

//            using var client = new SshClient(ip, SwitchUser, SwitchPass);
//            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(SshTimeoutSec);
//            client.Connect();

//            if (!client.IsConnected) return finalInfos;

//            using var stream = client.CreateShellStream("dumb", 80, 24, 800, 600, 1024);

//            // 0. آماده‌سازی ترمینال
//            SendCommand(stream, "terminal length 0");

//            // 1. دریافت لیست پورت‌های ترانک
//            var trunkPorts = GetTrunkPorts(stream);

//            // 2. دریافت جدول مک آدرس
//            var macEntries = GetMacAddressTable(stream);

//            // 3. دریافت اطلاعات CDP برای پیدا کردن IPها
//            string cdpOutput = SendCommand(stream, "show cdp neighbors detail");

//            client.Disconnect();

//            // 4. گروه‌بندی بر اساس پورت برای تشخیص اینکه روی هر پورت چه دستگاه‌هایی است
//            var groupedByPort = macEntries.GroupBy(x => x.Port);

//            foreach (var group in groupedByPort)
//            {
//                var portName = group.Key;
//                var entries = group.ToList();

//                // بررسی اینکه آیا پورت ترانک است یا خیر
//                bool isTrunk = trunkPorts.Contains(portName);

//                // متغیرهای نگهدارنده
//                RawMacEntry pcEntry = null;
//                RawMacEntry phoneEntry = null;
//                RawMacEntry vtEntry = null;

//                foreach (var entry in entries)
//                {
//                    // *** اعمال قوانین ۴ گانه شما ***

//                    if (isTrunk)
//                    {
//                        // قانون 3 و 4: اگر ترانک است
//                        if (entry.Vlan == "200")
//                        {
//                            // قانون 4: ترانک و Vlan 200 -> VT
//                            vtEntry = entry;
//                        }
//                        else
//                        {
//                            // قانون 3: ترانک و Vlan غیر 200 -> نادیده بگیر (Break or Continue)
//                            continue;
//                        }
//                    }
//                    else
//                    {
//                        // قانون 1 و 2: اگر ترانک نیست (اکسس است)
//                        if (entry.Vlan == "30")
//                        {
//                            // قانون 1: اکسس و Vlan 30 -> تلفن
//                            phoneEntry = entry;
//                        }
//                        else
//                        {
//                            // قانون 2: اکسس و غیر Vlan 30 -> کامپیوتر (PC)
//                            pcEntry = entry;
//                        }
//                    }
//                }

//                // اگر هیچ چیزی روی پورت پیدا نشد، رد شو
//                if (pcEntry == null && phoneEntry == null && vtEntry == null) continue;

//                // تلاش برای پیدا کردن IP از طریق CDP
//                // اولویت با VT است، اگر نبود Phone
//                string deviceIp = ParseCdpIp(cdpOutput, portName);

//                finalInfos.Add(new SwithInfo
//                {
//                    SwitchIp = ip,
//                    SwitchPort = portName,

//                    // PC Info
//                    PcMac = FormatMac(pcEntry?.Mac),
//                    PcVlan = pcEntry?.Vlan,
//                    PcIp = null, // معمولا PC در CDP دیده نمی‌شود

//                    // Phone Info
//                    PhoneMac = FormatMac(phoneEntry?.Mac),
//                    PhoneVlan = phoneEntry?.Vlan,
//                    PhoneIp = (vtEntry == null && phoneEntry != null) ? deviceIp : null,

//                    // VT Info
//                    VTMac = FormatMac(vtEntry?.Mac),
//                    VTVlan = vtEntry?.Vlan,
//                    VTIP = (vtEntry != null) ? deviceIp : null,

//                    UserFullName = "" // بعدا پر می‌شود
//                });
//            }

//            return finalInfos;
//        }

//        // ============================================================
//        // متدهای کمکی SSH و Parse
//        // ============================================================

//        private static HashSet<string> GetTrunkPorts(ShellStream stream)
//        {
//            var trunks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            string output = SendCommand(stream, "show interfaces trunk");

//            // تشخیص پورت‌ها در خروجی سیسکو (معمولا ستون اول نام پورت است)
//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                // مثال خروجی: Gi1/0/1    on          802.1q         trunking      1
//                var match = Regex.Match(line, @"^(Gi|Fa|Te|GigabitEthernet|FastEthernet)\S+", RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    trunks.Add(match.Value);
//                }
//            }
//            return trunks;
//        }

//        private static List<RawMacEntry> GetMacAddressTable(ShellStream stream)
//        {
//            var list = new List<RawMacEntry>();
//            string output = SendCommand(stream, "show mac address-table");

//            // regex برای پارس کردن خطوط استاندارد مک آدرس
//            // مثال: 10    0050.56be.c065    DYNAMIC     Gi0/1
//            var regex = new Regex(@"^\s*(?<vlan>\d+)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\w+\s+(?<port>\S+)", RegexOptions.Multiline);

//            foreach (Match m in regex.Matches(output))
//            {
//                // پورت‌های CPU را نادیده می‌گیریم
//                if (m.Groups["port"].Value.Contains("CPU", StringComparison.OrdinalIgnoreCase)) continue;

//                list.Add(new RawMacEntry
//                {
//                    Vlan = m.Groups["vlan"].Value,
//                    Mac = m.Groups["mac"].Value,
//                    Port = m.Groups["port"].Value
//                });
//            }
//            return list;
//        }

//        private static string SendCommand(ShellStream stream, string command)
//        {
//            stream.WriteLine(command);
//            // صبر کوتاه برای اجرای دستور
//            Thread.Sleep(200);

//            StringBuilder output = new StringBuilder();
//            var buffer = new byte[4096];

//            // خواندن کامل استریم تا زمانی که دیتا هست
//            while (stream.DataAvailable)
//            {
//                int read = stream.Read(buffer, 0, buffer.Length);
//                output.Append(Encoding.UTF8.GetString(buffer, 0, read));
//                Thread.Sleep(50); // صبر کوچک برای اطمینان از رسیدن بقیه دیتا
//            }

//            return output.ToString();
//        }

//        private static string ParseCdpIp(string cdpFullOutput, string portName)
//        {
//            if (string.IsNullOrEmpty(cdpFullOutput)) return null;

//            // نرمال‌سازی نام پورت برای جستجو (مثلا Gi1/0/1 رو پیدا کنیم)
//            // ساده‌ترین راه: جدا کردن بلاک‌های CDP بر اساس جداکننده معمول سیسکو
//            var blocks = cdpFullOutput.Split(new[] { "-------------------------" }, StringSplitOptions.RemoveEmptyEntries);

//            foreach (var block in blocks)
//            {
//                // اگر این بلاک مربوط به پورت مورد نظر ماست
//                if (block.Contains(portName, StringComparison.OrdinalIgnoreCase))
//                {
//                    // دنبال IP بگرد
//                    var match = Regex.Match(block, @"IP address:\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
//                    if (match.Success)
//                    {
//                        return match.Groups[1].Value;
//                    }
//                }
//            }
//            return null;
//        }

//        // ============================================================
//        // ابزارهای کمکی IP و فرمت‌بندی
//        // ============================================================

//        public static List<string> GetIpRange(string startIp, string endIp)
//        {
//            uint start = IpToUint(startIp);
//            uint end = IpToUint(endIp);
//            var list = new List<string>();
//            for (uint ip = start; ip <= end; ip++) list.Add(UintToIp(ip));
//            return list;
//        }

//        public static uint IpToUint(string ip)
//        {
//            var segments = ip.Split('.').Select(byte.Parse).ToArray();
//            return (uint)(segments[0] << 24 | segments[1] << 16 | segments[2] << 8 | segments[3]);
//        }

//        public static string UintToIp(uint value)
//        {
//            return $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
//        }
//        private static string FormatMac(string raw)
//        {
//            if (string.IsNullOrEmpty(raw)) return null;
//            var clean = Regex.Replace(raw, "[^0-9a-fA-F]", "").ToUpper();
//            if (clean.Length != 12) return raw;
//            return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
//        }
//    }
//}





//using Renci.SshNet;
//using SqlDataExtention.Data;
//using SqlDataExtention.Entity;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//        public bool IsTrunk;
//        public string Type { get; set; }
//    }

//    public class AccessSwitch
//    {
//        public string Name { get; set; }
//        public string IP { get; set; }
//        public string User { get; set; }
//        public string Pass { get; set; }
//    }


//    public class NetworkMapperOptions
//    {
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 30;
//        public int MaxRetries { get; set; } = 2;
//        public bool VerboseLogging { get; set; } = true;
//        public int ShellReadIntervalMs { get; set; } = 120;
//        public int ShellReadTimeoutMs { get; set; } = 1500;
//    }


//    public static class NetworkMapper
//    {
//        // --------------------------------------------------------
//        // IP RANGE HELPERS
//        // --------------------------------------------------------
//        private static uint IpToUint(string ip)
//        {
//            var segments = ip.Split('.').Select(byte.Parse).ToArray();
//            return (uint)(segments[0] << 24 | segments[1] << 16 | segments[2] << 8 | segments[3]);
//        }

//        private static string UintToIp(uint value)
//        {
//            return string.Join(".",
//                (value >> 24) & 0xFF,
//                (value >> 16) & 0xFF,
//                (value >> 8) & 0xFF,
//                value & 0xFF);
//        }

//        private static List<string> GetIpRange(string startIp, string endIp)
//        {
//            uint start = IpToUint(startIp);
//            uint end = IpToUint(endIp);

//            if (end < start)
//                throw new ArgumentException("End IP must be >= Start IP.");

//            var list = new List<string>();
//            for (uint ip = start; ip <= end; ip++)
//                list.Add(UintToIp(ip));

//            return list;
//        }


//        // ======================================================================
//        // ===============================  ENTRY  ===============================
//        // ======================================================================


//        public static void InsertToDB(string startIp, string endIp, IProgress<int> progress)
//        {
//            var results = MapMacsOnAccessSwitches(startIp, endIp, progress);

//            if (results.Count > 0)
//            {
//                var helper = new DataInsertUpdateHelper();
//                bool ok = helper.InsertMappingResults(results);
//                Console.WriteLine(ok ? "درج انجام شد" : "خطا در درج");
//            }
//        }

//        // ======================================================================
//        // ======================= SCAN ALL SWITCHES ============================
//        // ======================================================================

//        private static List<SwithInfo> MapMacsOnAccessSwitches(
//    string startIp, string endIp, IProgress<int> progress)
//        {
//            var ips = GetIpRange(startIp, endIp);
//            int total = ips.Count;
//            int counter = 0;

//            var options = new NetworkMapperOptions();
//            var switches = new List<AccessSwitch>();
//            var macTables = new Dictionary<string, List<MacEntry>>();
//            var cdpOutputs = new Dictionary<string, string>();

//            foreach (var ip in ips)
//            {
//                switches.Add(new AccessSwitch
//                {
//                    Name = ip,
//                    IP = ip,
//                    User = "infosw",
//                    Pass = "Ii123456!"
//                });
//            }

//            foreach (var sw in switches)
//            {
//                try
//                {
//                    ConnectAndCollect(
//                        new SwitchCfg(sw.IP, sw.Name, sw.User, sw.Pass),
//                        macTables,
//                        cdpOutputs,
//                        options);
//                }
//                catch { }

//                counter++;
//                progress?.Report(counter);   // گزارش پیشرفت به UI
//            }

//            return MapMacsOnAccessSwitches(switches, macTables, cdpOutputs);
//        }


//        // ======================================================================
//        // ===================== FINAL MAC / PHONE / PC MAPPING =================
//        // ======================================================================


//        private static List<SwithInfo> MapMacsOnAccessSwitches(
//             List<AccessSwitch> switches,
//             Dictionary<string, List<MacEntry>> macTables,
//             Dictionary<string, string> cdpOutputs)
//        {
//            var results = new List<SwithInfo>();

//            foreach (var sw in switches)
//            {
//                if (!macTables.TryGetValue(sw.Name, out var macList))
//                    continue;

//                // گروه‌بندی بر اساس پورت فیزیکی
//                var grouped = macList.GroupBy(x => x.Port);

//                foreach (var g in grouped)
//                {
//                    var port = g.Key;
//                    var entries = g.ToList();

//                    // حذف مک‌های تکراری محض اطمینان
//                    var uniqueMacs = entries
//                        .GroupBy(x => x.Mac)
//                        .Select(mg => mg.First())
//                        .ToList();

//                    MacEntry phone = null;
//                    MacEntry pc = null;
//                    MacEntry vt = null; // Video/Trunk Device (Vlan 200)

//                    foreach (var entry in uniqueMacs)
//                    {
//                        // سناریوی 4: ترانک است و VLAN 200 -> میره تو VT
//                        if (entry.IsTrunk && entry.Vlan == "200")
//                        {
//                            vt = entry;
//                        }
//                        // سناریوی 1 و 2: ترانک نیست و VLAN 30 -> میره تو Phone
//                        else if (!entry.IsTrunk && entry.Vlan == "30")
//                        {
//                            phone = entry;
//                        }
//                        // سناریوی 2 و 3: ترانک نیست و VLAN غیر 30 -> میره تو PC
//                        else if (!entry.IsTrunk && entry.Vlan != "30")
//                        {
//                            pc = entry;
//                        }
//                    }

//                    // اگر هیچ مکی پیدا نشد، ادامه بده
//                    if (pc == null && phone == null && vt == null) continue;

//                    // دریافت IP از CDP
//                    string cdpIp = null;
//                    if (cdpOutputs.TryGetValue(sw.Name, out var cdpTxt))
//                        cdpIp = ParseCdpIpForYourSwitch(cdpTxt, port);

//                    // منطق تخصیص IP CDP:
//                    // اگر VT داریم، IP مال اونه.
//                    // اگر VT نداریم ولی Phone داریم، IP مال اونه.
//                    string vtIp = (vt != null) ? cdpIp : null;
//                    string phoneIp = (vt == null && phone != null) ? cdpIp : null;

//                    results.Add(new SwithInfo
//                    {
//                        SwitchIp = sw.IP,
//                        SwitchPort = port,

//                        // PC Info
//                        PcMac = FormatMac(pc?.Mac),
//                        PcVlan = pc?.Vlan,
//                        PcIp = null, // معمولا PC با CDP آی‌پی نمیدهد

//                        // Phone Info
//                        PhoneMac = FormatMac(phone?.Mac),
//                        PhoneVlan = phone?.Vlan,
//                        PhoneIp = phoneIp,

//                        // VT Info (Trunk Vlan 200)
//                        VTMac = FormatMac(vt?.Mac),
//                        VTVlan = vt?.Vlan,
//                        VTIP = vtIp,

//                        SystemInfoRef = 0
//                    });
//                }
//            }

//            return results;
//        }

//        // ======================================================================
//        // ========================== SSH COLLECT ================================
//        // ======================================================================

//        private static void ConnectAndCollect(
//            SwitchCfg sw,
//            Dictionary<string, List<MacEntry>> macTables,
//            Dictionary<string, string> cdpOutputs,
//            NetworkMapperOptions options)
//        {
//            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);

//            client.Connect();
//            if (!client.IsConnected)
//                throw new Exception("SSH connect failed");

//            using var stream = client.CreateShellStream("xterm", 80, 24, 800, 600, 4096);

//            WriteAndRead(stream, "terminal length 0", options);

//            var trunkSet = GetTrunkPortsSafe(stream, options);

//            string macOut = "";
//            var macCmds = new[]
//            {
//                "show mac address-table",
//                "show mac address-table dynamic",
//                "show mac address-table static"
//            };

//            foreach (var cmd in macCmds)
//            {
//                var txt = WriteAndRead(stream, cmd, options);
//                if (!string.IsNullOrWhiteSpace(txt) && LooksLikeMacTable(txt))
//                {
//                    macOut = txt;
//                    break;
//                }
//            }

//            if (string.IsNullOrWhiteSpace(macOut))
//                macOut = WriteAndRead(stream, "show mac address-table", options);

//            macTables[sw.Name] = ParseMacAddressTableFiltered(macOut, trunkSet);

//            cdpOutputs[sw.Name] =
//                WriteAndRead(stream, "show cdp neighbors detail", options) ?? "";

//            try { client.Disconnect(); } catch { }
//        }

//        // ======================================================================
//        // ========================== SHELL READ HELPERS ========================
//        // ======================================================================

//        private static string WriteAndRead(ShellStream stream, string cmd, NetworkMapperOptions options)
//        {
//            DrainStream(stream);
//            stream.WriteLine(cmd);
//            Thread.Sleep(options.ShellReadIntervalMs);

//            var sb = new StringBuilder();
//            var sw = System.Diagnostics.Stopwatch.StartNew();

//            int lastLen = 0;

//            while (sw.ElapsedMilliseconds < options.ShellReadTimeoutMs)
//            {
//                try
//                {
//                    if (stream.DataAvailable)
//                    {
//                        var chunk = stream.Read();
//                        if (!string.IsNullOrEmpty(chunk)) sb.Append(chunk);
//                    }
//                }
//                catch { }

//                if (sb.Length == lastLen)
//                {
//                    Thread.Sleep(options.ShellReadIntervalMs);
//                    if (sb.Length == lastLen) break;
//                }

//                lastLen = sb.Length;
//                Thread.Sleep(options.ShellReadIntervalMs);
//            }

//            return sb.ToString();
//        }

//        private static void DrainStream(ShellStream stream)
//        {
//            try
//            {
//                while (stream.DataAvailable)
//                    _ = stream.Read();
//            }
//            catch { }
//        }


//        // ======================================================================
//        // =============================== TRUNKS ================================
//        // ======================================================================

//        private static HashSet<string> GetTrunkPortsSafe(ShellStream stream, NetworkMapperOptions options)
//        {
//            var cmds = new[]
//            {
//                "show interfaces trunk",
//                "show interface trunk",
//                "show interface switchport"
//            };

//            foreach (var c in cmds)
//            {
//                var outp = WriteAndRead(stream, c, options);
//                var set = ParseTrunkPorts(outp);
//                if (set.Count > 0)
//                    return set;
//            }

//            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//        }

//        private static HashSet<string> ParseTrunkPorts(string output)
//        {
//            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

//            foreach (var l in lines)
//            {
//                var m = Regex.Match(l.Trim(),
//                    @"^(?<if>(?:Gi|Fa|Ten|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)\s+",
//                    RegexOptions.IgnoreCase);

//                if (m.Success)
//                    set.Add(m.Groups["if"].Value.Trim());
//            }

//            return set;
//        }


//        // ======================================================================
//        // ========================= PARSE MAC TABLE ============================
//        // ======================================================================

//        private static bool LooksLikeMacTable(string txt)
//        {
//            if (string.IsNullOrWhiteSpace(txt)) return false;

//            return Regex.IsMatch(txt,
//                @"^\s*(\d+|All)\s+[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\s+(STATIC|DYNAMIC)\s+\S+",
//                RegexOptions.Multiline);
//        }

//        //private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
//        //{
//        //    var rawList = new List<MacEntry>();
//        //    var lines = output.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

//        //    foreach (var raw in lines)
//        //    {
//        //        var line = raw.Trim();
//        //        if (string.IsNullOrWhiteSpace(line)) continue;
//        //        if (line.StartsWith("Vlan", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (line.StartsWith("---")) continue;
//        //        if (line.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) continue;

//        //        var m = Regex.Match(line,
//        //            @"^(?<vlan>\d+|All)\s+(?<mac>[0-9A-Fa-f\.]+)\s+(STATIC|DYNAMIC)\s+(?<port>(Gi|Fa|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)",
//        //            RegexOptions.IgnoreCase);

//        //        if (!m.Success) continue;

//        //        var vlan = m.Groups["vlan"].Value.Trim();
//        //        var mac = NormalizeMac(m.Groups["mac"].Value);
//        //        var port = m.Groups["port"].Value.Trim();

//        //        if (mac == null) continue;

//        //        // فیلتر موارد غیرضروری
//        //        if (vlan.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (port.Equals("CPU", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (trunkPorts.Contains(port) && vlan != "200") continue;


//        //        rawList.Add(new MacEntry
//        //        {
//        //            Vlan = vlan,
//        //            Mac = mac,
//        //            Port = port
//        //        });
//        //    }

//        //    // **اینجا کلیده! - گروه‌بندی بر اساس Port و Mac**
//        //    // هر مک که روی یک پورت چندبار دیده شده، فقط یکبار نگه داریم
//        //    var deduplicated = rawList
//        //        .GroupBy(x => new { x.Port, x.Mac })
//        //        .Select(g =>
//        //        {
//        //            // اولویت: اگر VLAN 30 وجود داشت، اونو بردار (تلفن)
//        //            // وگرنه اولین VLAN رو بردار
//        //            var preferred = g.FirstOrDefault(e => e.Vlan == "30") ?? g.First();
//        //            return preferred;
//        //        })
//        //        .ToList();

//        //    return deduplicated;
//        //}
//        //private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
//        //{
//        //    var rawList = new List<MacEntry>();
//        //    var lines = output.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

//        //    foreach (var raw in lines)
//        //    {
//        //        var line = raw.Trim();
//        //        if (string.IsNullOrWhiteSpace(line)) continue;
//        //        if (line.StartsWith("Vlan", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (line.StartsWith("---")) continue;
//        //        if (line.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) continue;

//        //        var m = Regex.Match(line,
//        //            @"^(?<vlan>\d+|All)\s+(?<mac>[0-9A-Fa-f\.]+)\s+(STATIC|DYNAMIC)\s+(?<port>(Gi|Fa|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)",
//        //            RegexOptions.IgnoreCase);

//        //        if (!m.Success) continue;

//        //        var vlan = m.Groups["vlan"].Value.Trim();
//        //        var mac = NormalizeMac(m.Groups["mac"].Value);
//        //        var port = m.Groups["port"].Value.Trim();

//        //        if (mac == null) continue;

//        //        // فیلترهای عمومی
//        //        if (vlan.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (port.Equals("CPU", StringComparison.OrdinalIgnoreCase)) continue;

//        //        // تشخیص ترانک بودن پورت
//        //        bool isTrunkPort = trunkPorts.Contains(port);

//        //        // *** قانون اصلی فیلتر شما ***
//        //        // اگر پورت ترانک است:
//        //        if (isTrunkPort)
//        //        {
//        //            // اگر VLAN 200 نیست، کلاً حذف کن (سناریوی 5 حذف میشود)
//        //            // فقط سناریوی 4 (ترانک روی 200) باقی میماند
//        //            if (vlan != "200")
//        //                continue;
//        //        }
//        //        // اگر پورت اکسس است (isTrunkPort == false)، همه چیز (Vlan 30 و غیره) باقی می‌ماند

//        //        rawList.Add(new MacEntry
//        //        {
//        //            Vlan = vlan,
//        //            Mac = mac,
//        //            Port = port,
//        //            IsTrunk = isTrunkPort
//        //        });
//        //    }

//        //    // حذف تکراری‌ها: اگر یک مک روی یک پورت چند بار دیده شده
//        //    // (مثلا یکبار داینامیک یکبار استاتیک و ...) فقط یکی را بردار
//        //    var deduplicated = rawList
//        //        .GroupBy(x => new { x.Port, x.Mac })
//        //        .Select(g => g.First())
//        //        .ToList();

//        //    return deduplicated;
//        //}
//        //private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
//        //{
//        //    var rawList = new List<MacEntry>();
//        //    var lines = output.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

//        //    foreach (var raw in lines)
//        //    {
//        //        var line = raw.Trim();
//        //        if (string.IsNullOrWhiteSpace(line)) continue;
//        //        if (line.StartsWith("Vlan", StringComparison.OrdinalIgnoreCase)) continue;
//        //        if (line.StartsWith("---")) continue;
//        //        if (line.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) continue;

//        //        var m = Regex.Match(line,
//        //            @"^(?<vlan>\d+|All)\s+(?<mac>[0-9A-Fa-f\.]+)\s+(STATIC|DYNAMIC)\s+(?<port>(Gi|Fa|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)",
//        //            RegexOptions.IgnoreCase);

//        //        if (!m.Success) continue;

//        //        var vlan = m.Groups["vlan"].Value.Trim();
//        //        var mac = NormalizeMac(m.Groups["mac"].Value);
//        //        var port = m.Groups["port"].Value.Trim();

//        //        if (mac == null) continue;

//        //        // حذف VLAN=ALL
//        //        if (vlan.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;

//        //        // حذف پورت CPU اگر دیده شود
//        //        if (port.Equals("CPU", StringComparison.OrdinalIgnoreCase)) continue;

//        //        bool isTrunkPort = trunkPorts.Contains(port);

//        //        // *** قوانین فیلتر ***
//        //        if (isTrunkPort)
//        //        {
//        //            // فقط VLAN=200 از ترانک اجازه دارد
//        //            if (vlan != "200")
//        //                continue;
//        //        }
//        //        // اگر ترانک نیست، همه VLAN ها مجاز هستند

//        //        rawList.Add(new MacEntry
//        //        {
//        //            Vlan = vlan,
//        //            Mac = mac,
//        //            Port = port,
//        //            IsTrunk = isTrunkPort
//        //        });
//        //    }

//        //    // حذف موارد تکراری (MAC + PORT)
//        //    var dedup = rawList
//        //        .GroupBy(x => new { x.Port, x.Mac })
//        //        .Select(g => g.First())
//        //        .ToList();

//        //    return dedup;
//        //}

//        public static List<MacEntry> ParseMacAddressTableFiltered(string macOutput)
//        {
//            var result = new List<MacEntry>();

//            if (string.IsNullOrWhiteSpace(macOutput))
//                return result;

//            var lines = macOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

//            // الگوی خطوط واقعی جدول MAC
//            // مثال:
//            //  11    7456.3c63.0fa8    DYNAMIC     Gi1/0/15
//            var regex = new Regex(
//                @"^\s*(?<vlan>\d+)\s+(?<mac>[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4})\s+(STATIC|DYNAMIC)\s+(?<port>[A-Za-z]+[\d/]+)",
//                RegexOptions.Compiled);

//            foreach (var line in lines)
//            {
//                var m = regex.Match(line);
//                if (!m.Success)
//                    continue;

//                var vlan = m.Groups["vlan"].Value.Trim();
//                var mac = NormalizeMac(m.Groups["mac"].Value);
//                var port = m.Groups["port"].Value.Trim();

//                if (string.IsNullOrWhiteSpace(vlan)) continue;
//                if (string.IsNullOrWhiteSpace(mac)) continue;
//                if (string.IsNullOrWhiteSpace(port)) continue;

//                result.Add(new MacEntry
//                {
//                    Vlan = vlan,
//                    Mac = mac,
//                    Port = port,
//                    IsTrunk = false // چون این نسخه فقط MAC را استخراج می‌کند
//                });
//            }

//            // حذف تکراری‌ها (MAC روی یک پورت فقط یکبار)
//            var dedup = result
//                .GroupBy(x => new { x.Port, x.Mac })
//                .Select(g => g.First())
//                .ToList();

//            return dedup;
//        }


//        // ======================================================================
//        // ====================== CDP / PHONE IP DETECTION ======================
//        // ======================================================================

//        private static string ToFullIfName(string shortIf)
//        {
//            if (string.IsNullOrWhiteSpace(shortIf)) return shortIf;

//            var s = shortIf.Trim();
//            s = Regex.Replace(s, @"^Gi(?=\d|/)", "GigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^Fa(?=\d|/)", "FastEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^Te(?=\d|/)", "TenGigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"\s*\(.*\)\s*$", "", RegexOptions.IgnoreCase);
//            return s.Trim().TrimEnd(',');
//        }

//        public static string ParseCdpIpForYourSwitch(string cdpOut, string localPort)
//        {
//            if (string.IsNullOrWhiteSpace(cdpOut) || string.IsNullOrWhiteSpace(localPort))
//                return null;

//            string normalizedFull = ToFullIfName(localPort).Trim();

//            var blocks = Regex.Split(cdpOut, @"(?=Device ID:)", RegexOptions.IgnoreCase);

//            foreach (var block in blocks)
//            {
//                if (block.Contains(localPort, StringComparison.OrdinalIgnoreCase) ||
//                    block.Contains(normalizedFull, StringComparison.OrdinalIgnoreCase))
//                {
//                    var m = Regex.Match(block,
//                        @"IP address:\s*([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
//                        RegexOptions.IgnoreCase);

//                    if (m.Success)
//                        return m.Groups[1].Value;
//                }
//            }

//            return null;
//        }

//        // ======================================================================
//        // ========================== NORMALIZE / FORMAT ========================
//        // ======================================================================

//        private static string NormalizeMac(string raw)
//        {
//            if (raw == null) return null;

//            var hex = Regex.Replace(raw, @"[^0-9A-Fa-f]", "");
//            if (hex.Length != 12) return null;

//            return hex.ToUpper();
//        }

//        public static string FormatMac(string raw)
//        {
//            raw = NormalizeMac(raw);
//            if (raw == null) return null;

//            return string.Join(":",
//                Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
//        }

//        public static List<string> GetIpRangePublic(string s, string e)
//        {
//            return GetIpRange(s, e);
//        }
//    }
//}

