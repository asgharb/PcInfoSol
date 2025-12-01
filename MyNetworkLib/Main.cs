using Renci.SshNet;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using System.Text;
using System.Text.RegularExpressions;

namespace MyNetworkLib
{
    public record SwitchCfg(string IP, string Name, string User, string Pass);

    public class MacEntry
    {
        public string Vlan;
        public string Mac;
        public string Port;
        public bool IsTrunk;
    }

    public class AccessSwitch
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
    }


    public class NetworkMapperOptions
    {
        public int SshTimeoutSeconds { get; set; } = 10;
        public int DelayBetweenSwitchMs { get; set; } = 30;
        public int MaxRetries { get; set; } = 2;
        public bool VerboseLogging { get; set; } = true;
        public int ShellReadIntervalMs { get; set; } = 120;
        public int ShellReadTimeoutMs { get; set; } = 1500;
    }


    public static class NetworkMapper
    {
        // --------------------------------------------------------
        // IP RANGE HELPERS
        // --------------------------------------------------------
        private static uint IpToUint(string ip)
        {
            var segments = ip.Split('.').Select(byte.Parse).ToArray();
            return (uint)(segments[0] << 24 | segments[1] << 16 | segments[2] << 8 | segments[3]);
        }

        private static string UintToIp(uint value)
        {
            return string.Join(".",
                (value >> 24) & 0xFF,
                (value >> 16) & 0xFF,
                (value >> 8) & 0xFF,
                value & 0xFF);
        }

        private static List<string> GetIpRange(string startIp, string endIp)
        {
            uint start = IpToUint(startIp);
            uint end = IpToUint(endIp);

            if (end < start)
                throw new ArgumentException("End IP must be >= Start IP.");

            var list = new List<string>();
            for (uint ip = start; ip <= end; ip++)
                list.Add(UintToIp(ip));

            return list;
        }


        // ======================================================================
        // ===============================  ENTRY  ===============================
        // ======================================================================


        public static void InsertToDB(string startIp, string endIp, IProgress<int> progress)
        {
            var results = MapMacsOnAccessSwitches(startIp, endIp, progress);

            if (results.Count > 0)
            {
                var helper = new DataInsertUpdateHelper();
                bool ok = helper.InsertMappingResults(results);
                Console.WriteLine(ok ? "درج انجام شد" : "خطا در درج");
            }
        }

        // ======================================================================
        // ======================= SCAN ALL SWITCHES ============================
        // ======================================================================

        private static List<SwithInfo> MapMacsOnAccessSwitches(
    string startIp, string endIp, IProgress<int> progress)
        {
            var ips = GetIpRange(startIp, endIp);
            int total = ips.Count;
            int counter = 0;

            var options = new NetworkMapperOptions();
            var switches = new List<AccessSwitch>();
            var macTables = new Dictionary<string, List<MacEntry>>();
            var cdpOutputs = new Dictionary<string, string>();

            foreach (var ip in ips)
            {
                switches.Add(new AccessSwitch
                {
                    Name = ip,
                    IP = ip,
                    User = "infosw",
                    Pass = "Ii123456!"
                });
            }

            foreach (var sw in switches)
            {
                try
                {
                    ConnectAndCollect(
                        new SwitchCfg(sw.IP, sw.Name, sw.User, sw.Pass),
                        macTables,
                        cdpOutputs,
                        options);
                }
                catch { }

                counter++;
                progress?.Report(counter);   // گزارش پیشرفت به UI
            }

            return MapMacsOnAccessSwitches(switches, macTables, cdpOutputs);
        }


        // ======================================================================
        // ===================== FINAL MAC / PHONE / PC MAPPING =================
        // ======================================================================


        private static List<SwithInfo> MapMacsOnAccessSwitches(
             List<AccessSwitch> switches,
             Dictionary<string, List<MacEntry>> macTables,
             Dictionary<string, string> cdpOutputs)
        {
            var results = new List<SwithInfo>();

            foreach (var sw in switches)
            {
                if (!macTables.TryGetValue(sw.Name, out var macList))
                    continue;

                // گروه‌بندی بر اساس پورت فیزیکی
                var grouped = macList.GroupBy(x => x.Port);

                foreach (var g in grouped)
                {
                    var port = g.Key;
                    var entries = g.ToList();

                    // حذف مک‌های تکراری محض اطمینان
                    var uniqueMacs = entries
                        .GroupBy(x => x.Mac)
                        .Select(mg => mg.First())
                        .ToList();

                    MacEntry phone = null;
                    MacEntry pc = null;
                    MacEntry vt = null; // Video/Trunk Device (Vlan 200)

                    foreach (var entry in uniqueMacs)
                    {
                        // سناریوی 4: ترانک است و VLAN 200 -> میره تو VT
                        if (entry.IsTrunk && entry.Vlan == "200")
                        {
                            vt = entry;
                        }
                        // سناریوی 1 و 2: ترانک نیست و VLAN 30 -> میره تو Phone
                        else if (!entry.IsTrunk && entry.Vlan == "30")
                        {
                            phone = entry;
                        }
                        // سناریوی 2 و 3: ترانک نیست و VLAN غیر 30 -> میره تو PC
                        else if (!entry.IsTrunk && entry.Vlan != "30")
                        {
                            pc = entry;
                        }
                    }

                    // اگر هیچ مکی پیدا نشد، ادامه بده
                    if (pc == null && phone == null && vt == null) continue;

                    // دریافت IP از CDP
                    string cdpIp = null;
                    if (cdpOutputs.TryGetValue(sw.Name, out var cdpTxt))
                        cdpIp = ParseCdpIpForYourSwitch(cdpTxt, port);

                    // منطق تخصیص IP CDP:
                    // اگر VT داریم، IP مال اونه.
                    // اگر VT نداریم ولی Phone داریم، IP مال اونه.
                    string vtIp = (vt != null) ? cdpIp : null;
                    string phoneIp = (vt == null && phone != null) ? cdpIp : null;

                    results.Add(new SwithInfo
                    {
                        SwitchIp = sw.IP,
                        SwitchPort = port,

                        // PC Info
                        PcMac = FormatMac(pc?.Mac),
                        PcVlan = pc?.Vlan,
                        PcIp = null, // معمولا PC با CDP آی‌پی نمیدهد

                        // Phone Info
                        PhoneMac = FormatMac(phone?.Mac),
                        PhoneVlan = phone?.Vlan,
                        PhoneIp = phoneIp,

                        // VT Info (Trunk Vlan 200)
                        VTMac = FormatMac(vt?.Mac),
                        VTVlan = vt?.Vlan,
                        VTIP = vtIp,

                        SystemInfoRef = 0
                    });
                }
            }

            return results;
        }

        // ======================================================================
        // ========================== SSH COLLECT ================================
        // ======================================================================

        private static void ConnectAndCollect(
            SwitchCfg sw,
            Dictionary<string, List<MacEntry>> macTables,
            Dictionary<string, string> cdpOutputs,
            NetworkMapperOptions options)
        {
            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);

            client.Connect();
            if (!client.IsConnected)
                throw new Exception("SSH connect failed");

            using var stream = client.CreateShellStream("xterm", 80, 24, 800, 600, 4096);

            WriteAndRead(stream, "terminal length 0", options);

            var trunkSet = GetTrunkPortsSafe(stream, options);

            string macOut = "";
            var macCmds = new[]
            {
                "show mac address-table",
                "show mac address-table dynamic",
                "show mac address-table static"
            };

            foreach (var cmd in macCmds)
            {
                var txt = WriteAndRead(stream, cmd, options);
                if (!string.IsNullOrWhiteSpace(txt) && LooksLikeMacTable(txt))
                {
                    macOut = txt;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(macOut))
                macOut = WriteAndRead(stream, "show mac address-table", options);

            macTables[sw.Name] = ParseMacAddressTableFiltered(macOut, trunkSet);

            cdpOutputs[sw.Name] =
                WriteAndRead(stream, "show cdp neighbors detail", options) ?? "";

            try { client.Disconnect(); } catch { }
        }

        // ======================================================================
        // ========================== SHELL READ HELPERS ========================
        // ======================================================================

        private static string WriteAndRead(ShellStream stream, string cmd, NetworkMapperOptions options)
        {
            DrainStream(stream);
            stream.WriteLine(cmd);
            Thread.Sleep(options.ShellReadIntervalMs);

            var sb = new StringBuilder();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int lastLen = 0;

            while (sw.ElapsedMilliseconds < options.ShellReadTimeoutMs)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        var chunk = stream.Read();
                        if (!string.IsNullOrEmpty(chunk)) sb.Append(chunk);
                    }
                }
                catch { }

                if (sb.Length == lastLen)
                {
                    Thread.Sleep(options.ShellReadIntervalMs);
                    if (sb.Length == lastLen) break;
                }

                lastLen = sb.Length;
                Thread.Sleep(options.ShellReadIntervalMs);
            }

            return sb.ToString();
        }

        private static void DrainStream(ShellStream stream)
        {
            try
            {
                while (stream.DataAvailable)
                    _ = stream.Read();
            }
            catch { }
        }


        // ======================================================================
        // =============================== TRUNKS ================================
        // ======================================================================

        private static HashSet<string> GetTrunkPortsSafe(ShellStream stream, NetworkMapperOptions options)
        {
            var cmds = new[]
            {
                "show interfaces trunk",
                "show interface trunk",
                "show interface switchport"
            };

            foreach (var c in cmds)
            {
                var outp = WriteAndRead(stream, c, options);
                var set = ParseTrunkPorts(outp);
                if (set.Count > 0)
                    return set;
            }

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> ParseTrunkPorts(string output)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var l in lines)
            {
                var m = Regex.Match(l.Trim(),
                    @"^(?<if>(?:Gi|Fa|Ten|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)\s+",
                    RegexOptions.IgnoreCase);

                if (m.Success)
                    set.Add(m.Groups["if"].Value.Trim());
            }

            return set;
        }


        // ======================================================================
        // ========================= PARSE MAC TABLE ============================
        // ======================================================================

        private static bool LooksLikeMacTable(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return false;

            return Regex.IsMatch(txt,
                @"^\s*(\d+|All)\s+[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\s+(STATIC|DYNAMIC)\s+\S+",
                RegexOptions.Multiline);
        }

        //private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
        //{
        //    var rawList = new List<MacEntry>();
        //    var lines = output.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var raw in lines)
        //    {
        //        var line = raw.Trim();
        //        if (string.IsNullOrWhiteSpace(line)) continue;
        //        if (line.StartsWith("Vlan", StringComparison.OrdinalIgnoreCase)) continue;
        //        if (line.StartsWith("---")) continue;
        //        if (line.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) continue;

        //        var m = Regex.Match(line,
        //            @"^(?<vlan>\d+|All)\s+(?<mac>[0-9A-Fa-f\.]+)\s+(STATIC|DYNAMIC)\s+(?<port>(Gi|Fa|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)",
        //            RegexOptions.IgnoreCase);

        //        if (!m.Success) continue;

        //        var vlan = m.Groups["vlan"].Value.Trim();
        //        var mac = NormalizeMac(m.Groups["mac"].Value);
        //        var port = m.Groups["port"].Value.Trim();

        //        if (mac == null) continue;

        //        // فیلتر موارد غیرضروری
        //        if (vlan.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
        //        if (port.Equals("CPU", StringComparison.OrdinalIgnoreCase)) continue;
        //        if (trunkPorts.Contains(port) && vlan != "200") continue;


        //        rawList.Add(new MacEntry
        //        {
        //            Vlan = vlan,
        //            Mac = mac,
        //            Port = port
        //        });
        //    }

        //    // **اینجا کلیده! - گروه‌بندی بر اساس Port و Mac**
        //    // هر مک که روی یک پورت چندبار دیده شده، فقط یکبار نگه داریم
        //    var deduplicated = rawList
        //        .GroupBy(x => new { x.Port, x.Mac })
        //        .Select(g =>
        //        {
        //            // اولویت: اگر VLAN 30 وجود داشت، اونو بردار (تلفن)
        //            // وگرنه اولین VLAN رو بردار
        //            var preferred = g.FirstOrDefault(e => e.Vlan == "30") ?? g.First();
        //            return preferred;
        //        })
        //        .ToList();

        //    return deduplicated;
        //}
        private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
        {
            var rawList = new List<MacEntry>();
            var lines = output.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("Vlan", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("---")) continue;
                if (line.StartsWith("Total", StringComparison.OrdinalIgnoreCase)) continue;

                var m = Regex.Match(line,
                    @"^(?<vlan>\d+|All)\s+(?<mac>[0-9A-Fa-f\.]+)\s+(STATIC|DYNAMIC)\s+(?<port>(Gi|Fa|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)",
                    RegexOptions.IgnoreCase);

                if (!m.Success) continue;

                var vlan = m.Groups["vlan"].Value.Trim();
                var mac = NormalizeMac(m.Groups["mac"].Value);
                var port = m.Groups["port"].Value.Trim();

                if (mac == null) continue;

                // فیلترهای عمومی
                if (vlan.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
                if (port.Equals("CPU", StringComparison.OrdinalIgnoreCase)) continue;

                // تشخیص ترانک بودن پورت
                bool isTrunkPort = trunkPorts.Contains(port);

                // *** قانون اصلی فیلتر شما ***
                // اگر پورت ترانک است:
                if (isTrunkPort)
                {
                    // اگر VLAN 200 نیست، کلاً حذف کن (سناریوی 5 حذف میشود)
                    // فقط سناریوی 4 (ترانک روی 200) باقی میماند
                    if (vlan != "200")
                        continue;
                }
                // اگر پورت اکسس است (isTrunkPort == false)، همه چیز (Vlan 30 و غیره) باقی می‌ماند

                rawList.Add(new MacEntry
                {
                    Vlan = vlan,
                    Mac = mac,
                    Port = port,
                    IsTrunk = isTrunkPort
                });
            }

            // حذف تکراری‌ها: اگر یک مک روی یک پورت چند بار دیده شده
            // (مثلا یکبار داینامیک یکبار استاتیک و ...) فقط یکی را بردار
            var deduplicated = rawList
                .GroupBy(x => new { x.Port, x.Mac })
                .Select(g => g.First())
                .ToList();

            return deduplicated;
        }


        // ======================================================================
        // ====================== CDP / PHONE IP DETECTION ======================
        // ======================================================================

        private static string ToFullIfName(string shortIf)
        {
            if (string.IsNullOrWhiteSpace(shortIf)) return shortIf;

            var s = shortIf.Trim();
            s = Regex.Replace(s, @"^Gi(?=\d|/)", "GigabitEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Fa(?=\d|/)", "FastEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Te(?=\d|/)", "TenGigabitEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s*\(.*\)\s*$", "", RegexOptions.IgnoreCase);
            return s.Trim().TrimEnd(',');
        }

        public static string ParseCdpIpForYourSwitch(string cdpOut, string localPort)
        {
            if (string.IsNullOrWhiteSpace(cdpOut) || string.IsNullOrWhiteSpace(localPort))
                return null;

            string normalizedFull = ToFullIfName(localPort).Trim();

            var blocks = Regex.Split(cdpOut, @"(?=Device ID:)", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (block.Contains(localPort, StringComparison.OrdinalIgnoreCase) ||
                    block.Contains(normalizedFull, StringComparison.OrdinalIgnoreCase))
                {
                    var m = Regex.Match(block,
                        @"IP address:\s*([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                        RegexOptions.IgnoreCase);

                    if (m.Success)
                        return m.Groups[1].Value;
                }
            }

            return null;
        }

        // ======================================================================
        // ========================== NORMALIZE / FORMAT ========================
        // ======================================================================

        private static string NormalizeMac(string raw)
        {
            if (raw == null) return null;

            var hex = Regex.Replace(raw, @"[^0-9A-Fa-f]", "");
            if (hex.Length != 12) return null;

            return hex.ToUpper();
        }

        public static string FormatMac(string raw)
        {
            raw = NormalizeMac(raw);
            if (raw == null) return null;

            return string.Join(":",
                Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
        }


        public static List<string> GetIpRangePublic(string s, string e)
        {
            return GetIpRange(s, e);
        }
    }
}