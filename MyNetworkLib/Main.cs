
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MyNetworkLib
{
    public record SwitchCfg(string IP, string Name, string User, string Pass);

    public class MacEntry
    {
        public string Vlan;
        public string Mac;
        public string Port;
    }

    public class MappingResult
    {
        public string Mac;          // normalized
        public string FoundSwitch;
        public string FoundPort;
        public string Vlan;

        public string PhoneMac;
        public string PhoneVlan;
        public string PhoneIp;

        public string Status; // FOUND / NOT FOUND / ERROR
    }

    public class NetworkMapperOptions
    {
        public int MaxParallelSsh { get; set; } = Math.Max(4, Environment.ProcessorCount * 2);
        public int SshTimeoutSeconds { get; set; } = 10;
        public int DelayBetweenSwitchMs { get; set; } = 30;
        public int MaxRetries { get; set; } = 2;
        public bool VerboseLogging { get; set; } = true;
        public int ShellReadIntervalMs { get; set; } = 120;
        public int ShellReadTimeoutMs { get; set; } = 1500;
    }

    public static class NetworkMapper
    {
        /// <summary>
        /// Scans switches (startIp..endIp) in parallel, collects MAC table and CDP, and maps given MACs to switch/port/vlan + phone IP if found.
        /// </summary>
        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(
            List<string> macs,
            NetworkMapperOptions? optionsIn = null,
            int startIp = 1,
            int endIp = 200,
            string user = "infosw",
            string pass = "Ii123456!")
        {
            var options = optionsIn ?? new NetworkMapperOptions();

            var switches = new List<SwitchCfg>();
            for (int i = startIp; i <= endIp; i++)
                switches.Add(new SwitchCfg($"192.168.254.{i}", $"SW-{i}", user, pass));

            var macTables = new ConcurrentDictionary<string, List<MacEntry>>();
            var cdpOutputs = new ConcurrentDictionary<string, string>();

            if (options.VerboseLogging)
                Console.WriteLine($"[Mapper] scanning {switches.Count} switches with parallel={options.MaxParallelSsh}");

            var po = new ParallelOptions { MaxDegreeOfParallelism = options.MaxParallelSsh };
            await Parallel.ForEachAsync(switches, po, async (sw, ct) =>
            {
                int attempt = 0;
                while (!ct.IsCancellationRequested)
                {
                    attempt++;
                    try
                    {
                        await ConnectAndCollectAsync(sw, macTables, cdpOutputs, options, ct);
                        break;
                    }
                    catch (SshException ex)
                    {
                        if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] SSH error attempt {attempt}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] error attempt {attempt}: {ex.Message}");
                    }

                    if (attempt >= options.MaxRetries)
                    {
                        macTables[sw.Name] = new List<MacEntry>();
                        cdpOutputs[sw.Name] = string.Empty;
                        if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] giving up after {attempt} attempts.");
                        break;
                    }

                    await Task.Delay(300 + new Random().Next(0, 200), ct);
                }

                // small gap to avoid bursts
                await Task.Delay(options.DelayBetweenSwitchMs, ct);
            });

            if (options.VerboseLogging)
            {
                Console.WriteLine("[Mapper] scan finished. Summary:");
                foreach (var kv in macTables.OrderBy(k => k.Key))
                    Console.WriteLine($"  {kv.Key}: {kv.Value.Count} mac rows, cdp present: {cdpOutputs.TryGetValue(kv.Key, out var v) && !string.IsNullOrWhiteSpace(v)}");
            }

            // Normalize requested MACs
            var normalizedMacs = macs.Select(m => NormalizeMac(m)).Where(m => m != null).ToList();
            var results = new List<MappingResult>(normalizedMacs.Count);

            foreach (var mac in normalizedMacs)
            {
                var r = new MappingResult { Mac = mac, Status = "ERROR" };

                var found = macTables.SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
                                     .FirstOrDefault(x => x.Entry.Mac != null && string.Equals(x.Entry.Mac, mac, StringComparison.OrdinalIgnoreCase));

                if (found == null)
                {
                    r.Status = "NOT FOUND";
                    if (options.VerboseLogging) Console.WriteLine($"[Mapper] MAC {mac} NOT FOUND");
                    results.Add(r);
                    continue;
                }

                r.Status = "FOUND";
                r.FoundSwitch = found.Switch;
                r.FoundPort = found.Entry.Port;
                r.Vlan = found.Entry.Vlan;

                if (options.VerboseLogging) Console.WriteLine($"[Mapper] MAC {mac} found on {r.FoundSwitch} {r.FoundPort} vlan {r.Vlan}");

                // find all macs on same port
                var samePortMacs = macTables[found.Switch].Where(e => string.Equals(e.Port, found.Entry.Port, StringComparison.OrdinalIgnoreCase)).ToList();

                // phone detection heuristic: VLAN 30 (user said phones are on vlan 30)
                var phone = samePortMacs.FirstOrDefault(e => e.Vlan == "30");
                if (phone != null)
                {
                    r.PhoneMac = FormatMacReadable(phone.Mac);
                    r.PhoneVlan = phone.Vlan;

                    if (cdpOutputs.TryGetValue(found.Switch, out var cdpOut) && !string.IsNullOrWhiteSpace(cdpOut))
                    {
                        var portFull = ToFullIfName(phone.Port);  // ← جایگزین found.Entry.Port
                        r.PhoneIp = ParseCdpIpForYourSwitch(cdpOut, portFull);
                    }
                }


                results.Add(r);
            }

            return results;
        }

        // ---------------- core per-switch logic ----------------
        private static async Task ConnectAndCollectAsync(
            SwitchCfg sw,
            ConcurrentDictionary<string, List<MacEntry>> macTables,
            ConcurrentDictionary<string, string> cdpOutputs,
            NetworkMapperOptions options,
            CancellationToken ct)
        {
            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);

            client.Connect();
            if (!client.IsConnected)
                throw new Exception("SSH connect failed");

            using var stream = client.CreateShellStream("xterm", 80, 24, 800, 600, 4096);

            // try to enter non-paging
            await WriteAndReadAsync(stream, "terminal length 0", options);

            // get trunk ports
            var trunkSet = await GetTrunkPortsSafeAsync(stream, options);

            // get mac table (try a few variants — shellstream)
            string macOut = string.Empty;
            var macCmds = new[] { "show mac address-table", "show mac address-table dynamic", "show mac address-table static" };
            foreach (var cmd in macCmds)
            {
                var txt = await WriteAndReadAsync(stream, cmd, options);
                if (!string.IsNullOrWhiteSpace(txt) && LooksLikeMacTable(txt))
                {
                    macOut = txt;
                    if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] mac from '{cmd}' len={txt.Length}");
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(macOut))
                macOut = await WriteAndReadAsync(stream, "show mac address-table", options);

            var macEntries = ParseMacAddressTableFiltered(macOut, trunkSet);
            macTables[sw.Name] = macEntries;

            // get CDP neighbors once
            var cdpOut = await WriteAndReadAsync(stream, "show cdp neighbors detail", options);
            cdpOutputs[sw.Name] = cdpOut ?? string.Empty;

            // disconnect politely
            try { client.Disconnect(); } catch { }
        }

        // ---------------- ShellStream helpers ----------------

        // write then collect output until quiet or timeout
        private static async Task<string> WriteAndReadAsync(ShellStream stream, string cmd, NetworkMapperOptions options)
        {
            // drain any pending data
            DrainStream(stream);

            // send command
            stream.WriteLine(cmd);
            await Task.Delay(options.ShellReadIntervalMs);

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
                catch { /* ignore */ }

                // if no growth for two intervals, consider stable and break early
                if (sb.Length == lastLen)
                {
                    await Task.Delay(options.ShellReadIntervalMs);
                    // check again; if still no change, break
                    if (sb.Length == lastLen) break;
                }
                lastLen = sb.Length;

                await Task.Delay(options.ShellReadIntervalMs);
            }

            return sb.ToString();
        }

        private static void DrainStream(ShellStream stream)
        {
            try
            {
                while (stream.DataAvailable)
                {
                    _ = stream.Read();
                }
            }
            catch { }
        }

        // ---------------- parsing helpers ----------------

        private static async Task<HashSet<string>> GetTrunkPortsSafeAsync(ShellStream stream, NetworkMapperOptions options)
        {
            try
            {
                // Try common trunk command variants
                var cmds = new[] { "show interfaces trunk", "show interface trunk", "show interface switchport" };
                foreach (var c in cmds)
                {
                    var outp = await WriteAndReadAsync(stream, c, options);
                    if (!string.IsNullOrWhiteSpace(outp) && outp.Length > 10)
                    {
                        var set = ParseTrunkPorts(outp);
                        if (set.Count > 0) return set;
                    }
                }
            }
            catch (Exception ex)
            {
                if (options.VerboseLogging) Console.WriteLine($"GetTrunkPortsSafeAsync failed: {ex.Message}");
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> ParseTrunkPorts(string output)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(output)) return set;

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines)
            {
                var s = l.Trim();
                // heuristic: first column often interface
                var m = Regex.Match(s, @"^(?<if>(?:Gi|Fa|Ten|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)\s+", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    set.Add(m.Groups["if"].Value.Trim());
                }
            }
            return set;
        }

        private static bool LooksLikeMacTable(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return false;
            return Regex.IsMatch(txt, @"\b(Vlan|VLAN|vlan)\b.*\b(Port|Gi|Fa|GigabitEthernet|FastEthernet)\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
        {
            var list = new List<MacEntry>();
            if (string.IsNullOrWhiteSpace(output)) return list;

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (Regex.IsMatch(line, "^Vlan\\b", RegexOptions.IgnoreCase) || line.StartsWith("----")) continue;

                // extract port (common at end)
                var portM = Regex.Match(line, @"(?<port>(?:Gi|Fa|Ten|Te|GigabitEthernet|FastEthernet|TenGigabitEthernet)\S+)$", RegexOptions.IgnoreCase);
                if (!portM.Success) continue;
                var port = portM.Groups["port"].Value;
                if (trunkPorts.Contains(port)) continue;
                if (string.Equals(port, "CPU", StringComparison.OrdinalIgnoreCase)) continue;

                // extract mac (various formats)
                var macM = Regex.Match(line, @"(?<mac>[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}|[0-9A-Fa-f]{12}|[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5})");
                if (!macM.Success) continue;
                var macRaw = macM.Groups["mac"].Value;

                // vlan at beginning
                var vlanM = Regex.Match(line, "^(?<vlan>All|\\d+)\\b", RegexOptions.IgnoreCase);
                var vlan = vlanM.Success ? vlanM.Groups["vlan"].Value : "Unknown";

                var macNorm = NormalizeMac(macRaw);
                if (macNorm == null) continue;

                list.Add(new MacEntry { Vlan = vlan, Mac = macNorm, Port = port });
            }

            return list;
        }

        private static string ToFullIfName(string shortIf)
        {
            if (string.IsNullOrWhiteSpace(shortIf)) return shortIf;
            var s = shortIf.Trim();

            // replace common short forms only when followed by digit or '/'
            s = Regex.Replace(s, @"^Gi(?=\d|\/)", "GigabitEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^G(?=\d|\/)", "GigabitEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Fa(?=\d|\/)", "FastEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^F(?=\d|\/)", "FastEthernet", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"^Te(?=\d|\/)", "TenGigabitEthernet", RegexOptions.IgnoreCase);

            // remove trailing parenthesis content like " (Full Duplex)"
            s = Regex.Replace(s, @"\s*\(.*\)\s*$", "", RegexOptions.IgnoreCase);
            // trim commas/spaces at end
            s = s.Trim().TrimEnd(',');

            return s;
        }

        private static string NormalizeMac(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var hex = Regex.Replace(raw.Trim(), "[^0-9a-fA-F]", "");
            return hex.Length == 12 ? hex.ToLower() : null;
        }

        // Parse CDP blocks and return IP for the local interface 'portFull' (portFull should be normalized via ToFullIfName/NormalizeIfName)
        public static string ParseCdpIpForYourSwitch(string cdpOut, string localPort)
        {
            if (string.IsNullOrWhiteSpace(cdpOut) || string.IsNullOrWhiteSpace(localPort))
                return null;

            string normalizedShort = localPort.Trim();
            string normalizedFull = ToFullIfName(localPort).Trim();

            // تقسیم بلاک‌ها (IOS معمولاً از ----- یا Device ID جدا می‌کند)
            var blocks = Regex.Split(cdpOut, @"(?=Device ID:)", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                // سوییچ باید پورت خودش را در این بلاک ذکر کرده باشد (local interface)
                if (
                    block.Contains(normalizedShort, StringComparison.OrdinalIgnoreCase) ||
                    block.Contains(normalizedFull, StringComparison.OrdinalIgnoreCase)
                )
                {
                    // استخراج آدرس IP از همان بلاک
                    var m = Regex.Match(block, @"IP address:\s*([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                        return m.Groups[1].Value;
                }
            }

            return null;
        }

        // Helper for interface normalization
        public static string NormalizeInterfaceName(string shortName)
        {
            shortName = shortName.Trim();

            if (shortName.StartsWith("Gi"))
                return shortName.Replace("Gi", "GigabitEthernet");
            if (shortName.StartsWith("Fa"))
                return shortName.Replace("Fa", "FastEthernet");
            if (shortName.StartsWith("Te"))
                return shortName.Replace("Te", "TenGigabitEthernet");

            return shortName;
        }

        private static string FormatMacReadable(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac)) return mac;

            var clean = Regex.Replace(mac, "[^0-9a-fA-F]", "").ToLower();
            if (clean.Length != 12) return mac;

            // فرمت اَنگلی Cisco (0001.22ff.aabb)
            var ciscoStyle = $"{clean.Substring(0, 4)}.{clean.Substring(4, 4)}.{clean.Substring(8, 4)}";

            // فرمت استاندارد شبکه (00:01:22:FF:AA:BB)
            var colonStyle = string.Join(":", Enumerable.Range(0, 6)
                .Select(i => clean.Substring(i * 2, 2)).ToArray()).ToUpper();

            // در نهایت تصویری‌تر: Cisco-style ولی با uppercase
            return $"{clean.Substring(0, 4)}.{clean.Substring(4, 4)}.{clean.Substring(8, 4)}".ToUpper();
        }

    }
}





















//using Renci.SshNet;
//using Renci.SshNet.Common;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;


//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    { 
//        public string Vlan; 
//        public string Mac;
//        public string Port; 
//    }

//    public class MappingResult
//    {
//        public string Mac; 
//        public string FoundSwitch;
//        public string FoundPort; 
//        public string Vlan;
//        public string PhoneMac;
//        public string PhoneVlan; 
//        public string PhoneIp;
//        public string Status;
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = Math.Max(4, Environment.ProcessorCount * 2);
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 30;
//        public int MaxRetries { get; set; } = 2;
//        public bool VerboseLogging { get; set; } = true;
//        public int ShellReadIntervalMs { get; set; } = 120; // read loop sleep
//        public int ShellReadTimeoutMs { get; set; } = 1200; // time to wait for output after command
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(
//            List<string> macs,
//            NetworkMapperOptions? optionsIn = null,
//            int startIp = 2,
//            int endIp = 2,
//            string user = "infosw",
//            string pass = "Ii123456!")
//        {
//            var options = optionsIn ?? new NetworkMapperOptions();

//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = startIp; i <= endIp; i++)
//                switchCfgs.Add(new SwitchCfg($"192.168.254.{i}", $"SW-{i}", user, pass));

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchCdpOutputs = new ConcurrentDictionary<string, string>();

//            if (options.VerboseLogging) Console.WriteLine($"Scanning {switchCfgs.Count} switches parallel={options.MaxParallelSsh}");

//            await Parallel.ForEachAsync(switchCfgs,
//                new ParallelOptions { MaxDegreeOfParallelism = options.MaxParallelSsh },
//                async (sw, ct) =>
//                {
//                    int attempt = 0;
//                    while (!ct.IsCancellationRequested)
//                    {
//                        attempt++;
//                        try
//                        {
//                            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);

//                            client.Connect();
//                            if (!client.IsConnected)
//                                throw new Exception("SSH connect failed");

//                            using var stream = client.CreateShellStream("xterm", 80, 24, 800, 600, 4096);

//                            // disable paging
//                            await WriteAndReadAsync(stream, "terminal length 0", options);

//                            // get trunk ports via stream (safe)
//                            var trunkSet = await GetTrunkPortsSafeAsync(stream, options);

//                            // get mac table via stream (try variants)
//                            string macOut = string.Empty;
//                            var macCmds = new[] { "show mac address-table", "show mac address-table dynamic", "show mac address-table static" };
//                            foreach (var cmd in macCmds)
//                            {
//                                var txt = await WriteAndReadAsync(stream, cmd, options);
//                                if (!string.IsNullOrWhiteSpace(txt) && LooksLikeMacTable(txt))
//                                {
//                                    macOut = txt;
//                                    if (options.VerboseLogging) Console.WriteLine($"{sw.Name} ({sw.IP}): mac from '{cmd}' len={txt.Length}");
//                                    break;
//                                }
//                            }
//                            if (string.IsNullOrWhiteSpace(macOut))
//                            {
//                                macOut = await WriteAndReadAsync(stream, "show mac address-table", options);
//                            }

//                            var macEntries = ParseMacAddressTableFiltered(macOut, trunkSet);
//                            switchMacTables[sw.Name] = macEntries;

//                            // get CDP once
//                            var cdpOut = await WriteAndReadAsync(stream, "show cdp neighbors detail", options);
//                            switchCdpOutputs[sw.Name] = cdpOut ?? string.Empty;

//                            if (options.VerboseLogging)
//                                Console.WriteLine($"{sw.Name} ({sw.IP}): macRows={macEntries.Count}, cdpLen={cdpOut?.Length ?? 0}");

//                            client.Disconnect();
//                            break;
//                        }
//                        catch (SshException ex)
//                        {
//                            if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] SSH error attempt {attempt}: {ex.Message}");
//                            if (attempt >= options.MaxRetries) { switchMacTables[sw.Name] = new List<MacEntry>(); switchCdpOutputs[sw.Name] = string.Empty; break; }
//                            await Task.Delay(300 + new Random().Next(0, 200), ct);
//                        }
//                        catch (Exception ex)
//                        {
//                            if (options.VerboseLogging) Console.WriteLine($"[{sw.Name}] attempt {attempt} failed: {ex.Message}");
//                            if (attempt >= options.MaxRetries) { switchMacTables[sw.Name] = new List<MacEntry>(); switchCdpOutputs[sw.Name] = string.Empty; break; }
//                            await Task.Delay(300 + new Random().Next(0, 200), ct);
//                        }
//                    } // while

//                    await Task.Delay(options.DelayBetweenSwitchMs, ct);
//                });

//            if (options.VerboseLogging)
//            {
//                Console.WriteLine("Scan finished. Summary:");
//                foreach (var kv in switchMacTables.OrderBy(k => k.Key))
//                    Console.WriteLine($" {kv.Key}: {kv.Value.Count} MAC rows");
//            }

//            // normalize inputs
//            var normalizedMacs = macs.Select(m => NormalizeMac(m)).Where(m => m != null).ToList();
//            var results = new List<MappingResult>(normalizedMacs.Count);

//            foreach (var mac in normalizedMacs)
//            {
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => x.Entry.Mac != null && string.Equals(x.Entry.Mac, mac, StringComparison.OrdinalIgnoreCase));

//                if (found == null) { r.Status = "NOT FOUND"; results.Add(r); if (options.VerboseLogging) Console.WriteLine($"MAC {mac} NOT FOUND"); continue; }

//                r.Status = "FOUND"; r.FoundSwitch = found.Switch; r.FoundPort = found.Entry.Port; r.Vlan = found.Entry.Vlan;
//                if (options.VerboseLogging) Console.WriteLine($"MAC {mac} -> {r.FoundSwitch} {r.FoundPort} vlan {r.Vlan}");

//                var allMacsSamePort = switchMacTables[found.Switch].Where(e => string.Equals(e.Port, found.Entry.Port, StringComparison.OrdinalIgnoreCase)).ToList();
//                var phone = allMacsSamePort.FirstOrDefault(e => e.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac; r.PhoneVlan = phone.Vlan;
//                    if (switchCdpOutputs.TryGetValue(found.Switch, out var cdpOut) && !string.IsNullOrWhiteSpace(cdpOut))
//                    {
//                        var portFull = ToFullIfName(r.FoundPort);
//                        r.PhoneIp = ParseCdpIpForYourSwitch(cdpOut, portFull);
//                    }
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        // ================= helpers - ShellStream based read/write =================

//        // write command and read until output quiesces (no new data for ShellReadTimeoutMs)
//        private static async Task<string> WriteAndReadAsync(ShellStream stream, string cmd, NetworkMapperOptions options)
//        {
//            // clear any buffered data first
//            DrainStream(stream);

//            stream.WriteLine(cmd);
//            // short initial wait for command to execute
//            await Task.Delay(options.ShellReadIntervalMs);

//            var sw = System.Diagnostics.Stopwatch.StartNew();
//            var sb = new System.Text.StringBuilder();
//            while (sw.ElapsedMilliseconds < options.ShellReadTimeoutMs)
//            {
//                try
//                {
//                    // read available
//                    if (stream.DataAvailable)
//                    {
//                        var chunk = stream.Read();
//                        if (!string.IsNullOrEmpty(chunk)) sb.Append(chunk);
//                        // continue loop to wait more data
//                    }
//                }
//                catch(Exception ex) 
//                {
//                    /* ignore read transient errors */
//                }

//                await Task.Delay(options.ShellReadIntervalMs);
//                // if no new data recently, we'll check timeout loop end and exit
//                // keep looping until overall timeout to allow multi-line outputs
//            }
//            return sb.ToString();
//        }

//        // try to quickly drain any pending bytes
//        private static void DrainStream(ShellStream stream)
//        {
//            try
//            {
//                while (stream.DataAvailable)
//                {
//                    var _ = stream.Read();
//                }
//            }
//            catch(Exception exp) 
//            { }
//        }

//        // get trunk ports using ShellStream (safe)
//        private static async Task<HashSet<string>> GetTrunkPortsSafeAsync(ShellStream stream, NetworkMapperOptions options)
//        {
//            try
//            {
//                var raw = await WriteAndReadAsync(stream, "show interfaces trunk", options);
//                return ParseTrunkPorts(raw);
//            }
//            catch (Exception ex)
//            {
//                if (options.VerboseLogging) Console.WriteLine($"GetTrunkPortsSafeAsync failed: {ex.Message}");
//                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            }
//        }

//        private static HashSet<string> ParseTrunkPorts(string output)
//        {
//            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            if (string.IsNullOrWhiteSpace(output)) return set;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var l in lines)
//            {
//                var s = l.Trim();
//                // line with interface name in first column (heuristic)
//                var m = Regex.Match(s, @"^(?<if>(?:Gi|Fa|GigabitEthernet|FastEthernet)\S+)\s+", RegexOptions.IgnoreCase);
//                if (m.Success) set.Add(m.Groups["if"].Value.Trim());
//            }
//            return set;
//        }

//        // heuristics to detect MAC table text
//        private static bool LooksLikeMacTable(string txt)
//            => !string.IsNullOrWhiteSpace(txt) && Regex.IsMatch(txt, @"\b(Vlan|VLAN|vlan)\b.*\b(Port|Gi|Fa|GigabitEthernet|FastEthernet)\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

//        // parse mac table and skip trunk ports
//        private static List<MacEntry> ParseMacAddressTableFiltered(string output, HashSet<string> trunkPorts)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var l in lines)
//            {
//                var line = l.Trim();
//                if (line.Length == 0) continue;
//                if (Regex.IsMatch(line, "^Vlan\\b", RegexOptions.IgnoreCase) || line.StartsWith("----")) continue;

//                var portM = Regex.Match(line, @"(?<port>(?:Gi|Fa|GigabitEthernet|FastEthernet|Ten)\S+)$", RegexOptions.IgnoreCase);
//                if (!portM.Success) continue;
//                var port = portM.Groups["port"].Value;
//                if (trunkPorts.Contains(port)) continue;
//                if (string.Equals(port, "CPU", StringComparison.OrdinalIgnoreCase)) continue;

//                var macM = Regex.Match(line, @"(?<mac>[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}|[0-9A-Fa-f]{12}|[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5})");
//                if (!macM.Success) continue;
//                var macRaw = macM.Groups["mac"].Value;
//                var vlanM = Regex.Match(line, "^(?<vlan>All|\\d+)\\b", RegexOptions.IgnoreCase);
//                var vlan = vlanM.Success ? vlanM.Groups["vlan"].Value : "Unknown";
//                var macNorm = NormalizeMac(macRaw);
//                if (macNorm == null) continue;

//                list.Add(new MacEntry { Vlan = vlan, Mac = macNorm, Port = port });
//            }
//            return list;
//        }

//        private static string ToFullIfName(string shortIf)
//        {
//            if (string.IsNullOrWhiteSpace(shortIf)) return shortIf;
//            var s = shortIf.Trim();
//            s = Regex.Replace(s, "^Gi\\b", "GigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, "^G\\b", "GigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, "^Fa\\b", "FastEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, "\\(.+\\)$", "", RegexOptions.IgnoreCase);
//            return s;
//        }

//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), "[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        // parse CDP output blocks (your switch format) to extract IP for interface
//        //private static string ParseCdpIpForYourSwitch(string cdpOutput, string portFull)
//        //{
//        //    if (string.IsNullOrWhiteSpace(cdpOutput) || string.IsNullOrWhiteSpace(portFull)) return null;

//        //    var blocks = Regex.Split(cdpOutput, "(?m)^[-]{4,}|(?m)^Device ID:")
//        //                      .Select(b => b.Trim())
//        //                      .Where(b => !string.IsNullOrWhiteSpace(b))
//        //                      .ToList();

//        //    foreach (var block in blocks)
//        //    {
//        //        var ifaceMatch = Regex.Match(block, @"Interface:\s*(?<if>[A-Za-z0-9\/]*)", RegexOptions.IgnoreCase);
//        //        if (!ifaceMatch.Success) continue;
//        //        var iface = ifaceMatch.Groups["if"].Value.Trim();
//        //        if (!string.Equals(iface, portFull, StringComparison.OrdinalIgnoreCase)) continue;

//        //        var ipMatch = Regex.Match(block, @"IP address:\s*(?<ip>\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.IgnoreCase);
//        //        if (ipMatch.Success) return ipMatch.Groups["ip"].Value;

//        //        // fallback: any ipv4 in block
//        //        ipMatch = Regex.Match(block, @"(?<ip>\d{1,3}(?:\.\d{1,3}){3})");
//        //        if (ipMatch.Success) return ipMatch.Groups["ip"].Value;
//        //    }

//        //    return null;
//        //}


//        private static string ParseCdpIpForYourSwitch(string cdpOutput, string portFull)
//        {
//            if (string.IsNullOrWhiteSpace(cdpOutput) || string.IsNullOrWhiteSpace(portFull))
//                return null;

//            portFull = NormalizeIfName(portFull);

//            // بلوک‌بندی درست: فقط بر اساس خطوط ----- جدا کن
//            var blocks = Regex.Split(cdpOutput, @"(?m)^-+\s*$")
//                              .Select(b => b.Trim())
//                              .Where(b => b.StartsWith("Device ID:", StringComparison.OrdinalIgnoreCase))
//                              .ToList();

//            foreach (var block in blocks)
//            {
//                // interface را با تطبیق فازی بگیر
//                var ifaceMatch = Regex.Match(block, @"Interface:\s*(?<if>[A-Za-z0-9\/\.]+)", RegexOptions.IgnoreCase);
//                if (!ifaceMatch.Success) continue;

//                var iface = NormalizeIfName(ifaceMatch.Groups["if"].Value);

//                // تطبیق فازی برابر بودن پورت
//                if (!iface.StartsWith(portFull, StringComparison.OrdinalIgnoreCase))
//                    continue;

//                // IP اصلی CDP
//                var ipMatch = Regex.Match(block, @"IP address:\s*(?<ip>\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.IgnoreCase);
//                if (ipMatch.Success) return ipMatch.Groups["ip"].Value;

//                // fallback هر IP موجود
//                ipMatch = Regex.Match(block, @"(?<ip>\d{1,3}(?:\.\d{1,3}){3})");
//                if (ipMatch.Success) return ipMatch.Groups["ip"].Value;
//            }

//            return null;
//        }

//        private static string NormalizeIfName(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw))
//                return string.Empty;

//            var s = raw.Trim();

//            // حذف کاما، پرانتز، حاشیه‌ها
//            s = Regex.Replace(s, @"[,\(\)].*$", "").Trim();

//            // کوتاه‌سازی‌ها → فرم کامل
//            s = Regex.Replace(s, @"^Gi\b", "GigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^G\b", "GigabitEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^Fa\b", "FastEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^F\b", "FastEthernet", RegexOptions.IgnoreCase);
//            s = Regex.Replace(s, @"^Te\b", "TenGigabitEthernet", RegexOptions.IgnoreCase);

//            return s;
//        }


//    }
//}














//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class MappingResult
//    {
//        public string Mac;          // MAC کامپیوتر
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;

//        public string PhoneMac;     // MAC تلفن VLAN30
//        public string PhoneVlan;
//        public string PhoneIp;      // IP تلفن

//        public string Status;       // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 8; // تعداد موازی
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 100;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // رنج سوئیچ‌ها
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 29; i <= 29; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW-{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            // جمع‌آوری MAC Table از همه سوئیچ‌ها
//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {

//                            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                            client.Connect();


//                            var macCmd = client.RunCommand("show mac address-table");
//                            await Task.Delay(200);


//                            // Parse all entries
//                            var allEntries = ParseMacAddressTable(macCmd.Result);


//                            // Filter out Gi1/0/25
//                            var filtered = allEntries.Where(e => !string.Equals(e.Port, "Gi1/0/25", StringComparison.OrdinalIgnoreCase)).ToList();


//                            // Save filtered version
//                            switchMacTables[sw.Name] = filtered;


//                            client.Disconnect();

//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                break;
//                            }
//                            await Task.Delay((int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200));
//                        }
//                    }
//                }
//                finally
//                {
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // ---- نگاشت MAC ها و تلفن ----
//            var results = new List<MappingResult>();

//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                // پیدا کردن MAC سیستم
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.Vlan = found.Entry.Vlan;
//                r.FoundPort = found.Entry.Port;

//                // همه MAC های روی همان پورت
//                var allMacsSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();


//                // پیدا کردن تلفن VLAN30
//                var phone = allMacsSamePort.FirstOrDefault(m => m.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;

//                    try
//                    {
//                        // IP سوئیچ
//                        string swIp = $"192.168.254.{found.Switch.Split('-')[1]}";

//                        using var client = new SshClient(swIp, "infosw", "Ii123456!");
//                        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
//                        client.Connect();

//                        // --- تعامل با سوئیچ (بدون enable) ---
//                        using var stream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);

//                        // خاموش کردن pagination
//                        stream.WriteLine("terminal length 0");
//                        Thread.Sleep(300);

//                        // تبدیل Gi/Fa به نام کامل
//                        string portFull = r.FoundPort
//                            .Replace("Gi", "GigabitEthernet")
//                            .Replace("Fa", "FastEthernet");

//                        // اجرای show cdp neighbors {port} detail
//                        stream.WriteLine($"show cdp neighbors {portFull} detail");
//                        Thread.Sleep(800);

//                        // خواندن خروجی کامل از stream
//                        string output = stream.Read();

//                        // اگر خروجی خالی بود، روش‌های جایگزین را امتحان کن
//                        if (string.IsNullOrWhiteSpace(output))
//                        {
//                            stream.WriteLine($"show cdp neighbors interface {portFull} detail");
//                            Thread.Sleep(800);
//                            output = stream.Read();
//                        }

//                        if (string.IsNullOrWhiteSpace(output))
//                        {
//                            stream.WriteLine("show cdp neighbors detail");
//                            Thread.Sleep(800);
//                            output = stream.Read();
//                        }

//                        // اگر داده داریم، استخراج IP تلفن از خروجی CDP
//                        if (!string.IsNullOrWhiteSpace(output))
//                        {
//                            r.PhoneIp = ParseCdpIp(output, phone.Mac);
//                        }

//                        client.Disconnect();
//                    }
//                    catch (Exception exp)
//                    {
//                        r.PhoneIp = null;
//                    }
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(),
//                    @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");

//                if (m.Success && !m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                {
//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }


//        private static string ParseCdpIp(string cdpOutput, string targetMac)
//        {
//            if (string.IsNullOrWhiteSpace(cdpOutput))
//                return null;

//            // ساده‌سازی: فقط دنبال IP Address می‌گردد
//            foreach (var line in cdpOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
//            {
//                var trimmed = line.Trim();

//                // در خروجی CDP همیشه خطی با "IP address:" داریم
//                if (trimmed.StartsWith("IP address:", StringComparison.OrdinalIgnoreCase))
//                {
//                    var ip = trimmed.Substring("IP address:".Length).Trim();
//                    if (System.Net.IPAddress.TryParse(ip, out _))  // فقط اگر واقعاً IP باشد
//                        return ip;
//                }

//                // بعضی نسخه‌ها ممکن است فقط "Management Address:" داشته باشند
//                if (trimmed.StartsWith("Management Address:", StringComparison.OrdinalIgnoreCase))
//                {
//                    var ip = trimmed.Substring("Management Address:".Length).Trim();
//                    if (System.Net.IPAddress.TryParse(ip, out _))
//                        return ip;
//                }
//            }

//            return null;
//        }
//    }
//}



















//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class MappingResult
//    {
//        public string Mac;          // MAC کامپیوتر
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;

//        public string PhoneMac;     // MAC تلفن VLAN30
//        public string PhoneVlan;
//        public string PhoneIp;      // IP تلفن

//        public string Status;       // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 8; // تعداد موازی
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 100;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // رنج سوئیچ‌ها
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 1; i <= 24; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW-{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            // جمع‌آوری MAC Table از همه سوئیچ‌ها
//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                            client.Connect();

//                            var macCmd = client.RunCommand("show mac address-table static");
//                            await Task.Delay(200);
//                            switchMacTables[sw.Name] = ParseMacAddressTable(macCmd.Result);

//                            client.Disconnect();
//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                break;
//                            }
//                            await Task.Delay((int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200));
//                        }
//                    }
//                }
//                finally
//                {
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // ---- نگاشت MAC ها و تلفن ----
//            var results = new List<MappingResult>();

//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                // پیدا کردن MAC سیستم
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.Vlan = found.Entry.Vlan;
//                r.FoundPort = found.Entry.Port;

//                // همه MAC های روی همان پورت
//                var allMacsSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();

//                // پیدا کردن تلفن VLAN30
//                var phone = allMacsSamePort.FirstOrDefault(m => m.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;

//                    try
//                    {
//                        // IP سوئیچ
//                        string swIp = $"192.168.254.{found.Switch.Split('-')[1]}";

//                        using var client = new SshClient(swIp, "infosw", "Ii123456!");
//                        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
//                        client.Connect();

//                        // ورود به enable mode
//                        client.RunCommand("enable");
//                        client.RunCommand("terminal length 0");

//                        // تبدیل Gi → GigabitEthernet
//                        string portFull =
//                            r.FoundPort.Replace("Gi", "GigabitEthernet")
//                                       .Replace("Fa", "FastEthernet");

//                        string cdpOutput = null;

//                        // --- روش ۱) پرکاربردترین ---
//                        var cmd1 = client.RunCommand($"show cdp neighbors {portFull} detail");
//                        if (!string.IsNullOrWhiteSpace(cmd1.Result))
//                            cdpOutput = cmd1.Result;

//                        // --- روش ۲) نسخه IOS قدیمی ---
//                        if (cdpOutput == null)
//                        {
//                            var cmd2 = client.RunCommand($"show cdp neighbors interface {portFull} detail");
//                            if (!string.IsNullOrWhiteSpace(cmd2.Result))
//                                cdpOutput = cmd2.Result;
//                        }
//                        // --- روش ۳) روش فول اسکن ---
//                        if (cdpOutput == null)
//                        {
//                            var cmd3 = client.RunCommand("show cdp neighbors detail");
//                            if (!string.IsNullOrWhiteSpace(cmd3.Result))
//                                cdpOutput = cmd3.Result;
//                        }

//                        if (cdpOutput != null)
//                        {
//                            r.PhoneIp = ParseCdpIp(cdpOutput, phone.Mac);
//                        }

//                        client.Disconnect();
//                    }
//                    catch(Exception exp)
//                    {
//                        r.PhoneIp = null;
//                    }
//                }

//                results.Add(r);
//            }


//            return results;
//        }

//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(), @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");
//                if (m.Success && !m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                {
//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }

//        private static string ParseCdpIp(string cdpOutput, string targetMac)
//        {
//            if (string.IsNullOrWhiteSpace(cdpOutput)) return null;
//            var lines = cdpOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            string currentMac = null;
//            string ip = null;
//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();
//                if (trimmed.StartsWith("Chassis id:"))
//                    currentMac = NormalizeMac(trimmed.Substring("Chassis id:".Length).Trim());
//                else if (trimmed.StartsWith("Management Address:") && currentMac == targetMac)
//                {
//                    ip = trimmed.Substring("Management Address:".Length).Trim();
//                    break;
//                }
//            }
//            return ip;
//        }
//    }
//}
















//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class LldpEntry
//    {
//        public string LocalPort;
//        public string NeighborDevice;
//        public string NeighborMac;
//        public string NeighborIp;
//    }

//    public class MappingResult
//    {
//        public string Mac;          // MAC کامپیوتر
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;

//        public string PhoneMac;     // MAC تلفن VLAN30
//        public string PhoneVlan;
//        public string PhoneIp;      // IP تلفن

//        public string Status;       // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 16; // تعداد کانکشن همزمان
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 50; // کاهش تاخیر
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // ایجاد رنج سوئیچ‌ها
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 24; i <= 24; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW-{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchLldpTables = new ConcurrentDictionary<string, List<LldpEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            // ---- جمع‌آوری جدول MAC ----
//                            List<MacEntry> macEntries = new List<MacEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var macCmd = client.RunCommand("show mac address-table static");
//                                await Task.Delay(50);
//                                macEntries = ParseMacAddressTable(macCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchMacTables[sw.Name] = macEntries;

//                            // ---- جمع‌آوری جدول LLDP/CDP ----
//                            List<LldpEntry> lldpEntries = new List<LldpEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var lldpCmd = client.RunCommand("show lldp neighbors detail");
//                                await Task.Delay(50);
//                                lldpEntries = ParseLldpTable(lldpCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchLldpTables[sw.Name] = lldpEntries;

//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                switchLldpTables[sw.Name] = new List<LldpEntry>();
//                                break;
//                            }
//                            await Task.Delay((int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200));
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // ---- نگاشت MAC ها و تلفن ----
//            var results = new List<MappingResult>();
//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                // پیدا کردن MAC سیستم
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.Vlan = found.Entry.Vlan;
//                r.FoundPort = found.Entry.Port;

//                // همه MAC های روی همان پورت
//                var allMacsSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();

//                // پیدا کردن تلفن VLAN30
//                var phone = allMacsSamePort.FirstOrDefault(m => m.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;

//                    // پیدا کردن IP تلفن از LLDP/CDP (Flexible)
//                    //var lldp1 = switchLldpTables[found.Switch]
//                    //    .FirstOrDefault(l => l.LocalPort == found.Entry.Port && l.NeighborMac == phone.Mac);


//                    var lldp2 = switchLldpTables[found.Switch]
//    .FirstOrDefault(l => l.LocalPort == found.Entry.Port && NormalizeMac(l.NeighborMac) == phone.Mac);



//                    var lldp3 = switchLldpTables[found.Switch]
//    .FirstOrDefault(l => NormalizeMac(l.NeighborMac) == phone.Mac);


//                    if (lldp2 != null)
//                        r.PhoneIp = lldp2.NeighborIp;
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        // ---- Helpers ----
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(), @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");
//                if (m.Success && !m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                {
//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }

//        private static List<LldpEntry> ParseLldpTable(string output)
//        {
//            var list = new List<LldpEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            LldpEntry current = null;
//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();
//                if (trimmed.StartsWith("Local Port:"))
//                {
//                    current = new LldpEntry { LocalPort = trimmed.Substring("Local Port:".Length).Trim() };
//                }
//                else if (current != null)
//                {
//                    if (trimmed.StartsWith("Chassis id:"))
//                        current.NeighborMac = NormalizeMac(trimmed.Substring("Chassis id:".Length).Trim());
//                    else if (trimmed.StartsWith("System Name:"))
//                        current.NeighborDevice = trimmed.Substring("System Name:".Length).Trim();
//                    else
//                    {
//                        // استخراج IP از هر خط که شبیه IPv4 باشد
//                        var ipMatch = Regex.Match(trimmed, @"(\d{1,3}\.){3}\d{1,3}");
//                        if (ipMatch.Success)
//                        {
//                            current.NeighborIp = ipMatch.Value;
//                            list.Add(current);
//                            current = null;
//                        }
//                    }
//                }
//            }

//            return list;
//        }
//    }
//}



//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class LldpEntry
//    {
//        public string LocalPort;
//        public string NeighborDevice;
//        public string NeighborMac;
//        public string NeighborIp;
//    }

//    public class MappingResult
//    {
//        public string Mac;          // MAC کامپیوتر
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;

//        public string PhoneMac;     // MAC تلفن VLAN30
//        public string PhoneVlan;
//        public string PhoneIp;      // IP تلفن

//        public string Status;       // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 4;
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 200;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // ایجاد رنج سوئیچ‌ها
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 24; i <= 24; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW-{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchLldpTables = new ConcurrentDictionary<string, List<LldpEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            // جمع‌آوری اطلاعات از سوئیچ‌ها
//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            // ---- جمع‌آوری جدول MAC ----
//                            List<MacEntry> macEntries = new List<MacEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var macCmd = client.RunCommand("show mac address-table static");
//                                await Task.Delay(200);
//                                macEntries = ParseMacAddressTable(macCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchMacTables[sw.Name] = macEntries;

//                            // ---- جمع‌آوری جدول LLDP/CDP ----
//                            List<LldpEntry> lldpEntries = new List<LldpEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var lldpCmd = client.RunCommand("show lldp neighbors detail");
//                                await Task.Delay(200);
//                                lldpEntries = ParseLldpTable(lldpCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchLldpTables[sw.Name] = lldpEntries;

//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                switchLldpTables[sw.Name] = new List<LldpEntry>();
//                                break;
//                            }
//                            await Task.Delay((int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200));
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // ---- نگاشت MAC ها و تلفن ----
//            var results = new List<MappingResult>();
//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                // پیدا کردن MAC سیستم
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.Vlan = found.Entry.Vlan;
//                r.FoundPort = found.Entry.Port;

//                // همه MAC های روی همان پورت
//                var allMacsSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();

//                // پیدا کردن تلفن VLAN30
//                var phone = allMacsSamePort.FirstOrDefault(m => m.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;

//                    // پیدا کردن IP تلفن از LLDP/CDP
//                    var lldp = switchLldpTables[found.Switch]
//                        .FirstOrDefault(l => l.LocalPort == found.Entry.Port && l.NeighborMac == phone.Mac);

//                    if (lldp != null)
//                        r.PhoneIp = lldp.NeighborIp;
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        // ---- Helpers ----
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(), @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");
//                if (m.Success)
//                {
//                    if (m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                        continue;

//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }

//        private static List<LldpEntry> ParseLldpTable(string output)
//        {
//            var list = new List<LldpEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            LldpEntry current = null;
//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();

//                if (trimmed.StartsWith("Local Port:"))
//                {
//                    current = new LldpEntry
//                    {
//                        LocalPort = trimmed.Substring("Local Port:".Length).Trim()
//                    };
//                }
//                else if (current != null)
//                {
//                    if (trimmed.StartsWith("Chassis id:"))
//                    {
//                        current.NeighborMac = NormalizeMac(trimmed.Substring("Chassis id:".Length).Trim());
//                    }
//                    else if (trimmed.StartsWith("System Name:"))
//                    {
//                        current.NeighborDevice = trimmed.Substring("System Name:".Length).Trim();
//                    }
//                    else if (trimmed.StartsWith("Management Address:"))
//                    {
//                        current.NeighborIp = trimmed.Substring("Management Address:".Length).Trim();
//                        list.Add(current);
//                        current = null;
//                    }
//                }
//            }

//            return list;
//        }
//    }
//}


















//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class LldpEntry
//    {
//        public string LocalPort;
//        public string NeighborMac;
//        public string NeighborDevice;
//    }

//    public class MappingResult
//    {
//        public string Mac;
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;
//        public string Status;

//        public string PhoneMac;
//        public string PhoneVlan;
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 5;
//        public int SshTimeoutSeconds { get; set; } = 8;
//        public int DelayBetweenSwitchMs { get; set; } = 150;
//        public int MaxRetries { get; set; } = 2;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // ✔ ایجاد لیست سوئیچ‌ها از 20 تا 72
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 20; i <= 72; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW-{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchLldpTables = new ConcurrentDictionary<string, List<LldpEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            // ---------------------------
//                            // MAC TABLE
//                            // ---------------------------
//                            List<MacEntry> macEntries;
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var cmd = client.RunCommand("show mac address-table");
//                                macEntries = ParseMacAddressTable(cmd.Result);

//                                client.Disconnect();
//                            }
//                            switchMacTables[sw.Name] = macEntries;

//                            // ---------------------------
//                            // LLDP TABLE
//                            // ---------------------------
//                            List<LldpEntry> lldpEntries;
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var cmd = client.RunCommand("show lldp neighbors detail");
//                                lldpEntries = ParseLldpTable(cmd.Result);

//                                client.Disconnect();
//                            }
//                            switchLldpTables[sw.Name] = lldpEntries;

//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                switchLldpTables[sw.Name] = new List<LldpEntry>();
//                                break;
//                            }
//                            await Task.Delay(300);
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // -----------------------------------------
//            //  پردازش MACها و نگاشت به پورت واقعی
//            // -----------------------------------------
//            var results = new List<MappingResult>();

//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "NOT FOUND" };

//                // 🎯 پیدا کردن MAC
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => x.Entry.Mac == mac);

//                if (found == null)
//                {
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.FoundPort = found.Entry.Port;
//                r.Vlan = found.Entry.Vlan;

//                // 🎯 پیدا کردن تلفن (VLAN 30) روی همان پورت
//                var allSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();

//                var phone = allSamePort.FirstOrDefault(m => m.Vlan == "30");
//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;
//                }

//                // 🎯 تأیید پورت توسط LLDP
//                var lldp = switchLldpTables[found.Switch]
//                    .FirstOrDefault(l => l.LocalPort == found.Entry.Port);

//                if (lldp != null)
//                    r.FoundPort = lldp.LocalPort;

//                results.Add(r);
//            }

//            return results;
//        }

//        // -----------------------------------------------------------
//        // Helpers
//        // -----------------------------------------------------------
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw, @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split('\n');
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(),
//                    @"^(?<vlan>\d+)\s+(?<mac>[0-9A-Fa-f\.]{14})\s+\S+\s+(?<port>\S+)$");

//                if (!m.Success) continue;
//                if (m.Groups["port"].Value.ToUpper() == "CPU") continue;

//                list.Add(new MacEntry
//                {
//                    Vlan = m.Groups["vlan"].Value,
//                    Mac = NormalizeMac(m.Groups["mac"].Value),
//                    Port = m.Groups["port"].Value
//                });
//            }
//            return list;
//        }

//        private static List<LldpEntry> ParseLldpTable(string output)
//        {
//            var list = new List<LldpEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split('\n');

//            LldpEntry entry = null;

//            foreach (var line in lines)
//            {
//                var t = line.Trim();

//                if (t.StartsWith("Local Port:"))
//                {
//                    entry = new LldpEntry();
//                    entry.LocalPort = t.Replace("Local Port:", "").Trim();
//                }
//                else if (t.StartsWith("Chassis id:") && entry != null)
//                {
//                    entry.NeighborMac = NormalizeMac(t.Replace("Chassis id:", "").Trim());
//                }
//                else if (t.StartsWith("System Name:") && entry != null)
//                {
//                    entry.NeighborDevice = t.Replace("System Name:", "").Trim();
//                    list.Add(entry);
//                    entry = null;
//                }
//            }

//            return list;
//        }
//    }
//}








//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class LldpEntry
//    {
//        public string LocalPort;
//        public string NeighborDevice;
//        public string NeighborMac;
//    }

//    public class MappingResult
//    {
//        public string Mac;
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;
//        public string Status;

//        public string PhoneMac;   
//        public string PhoneVlan;  
//    }


//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 4;
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 200;
//        public int CommandSpacingMs { get; set; } = 150;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // لیست سوئیچ‌ها برای تست
//            var switchCfgs = new List<SwitchCfg>();
//            switchCfgs.Add(new SwitchCfg("192.168.254.24", "SW24", "infosw", "Ii123456!"));

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchLldpTables = new ConcurrentDictionary<string, List<LldpEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            // ---- جمع‌آوری جدول MAC ----
//                            List<MacEntry> macEntries = new List<MacEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var macCmd = client.RunCommand("show mac address-table static");
//                                await Task.Delay(200);
//                                macEntries = ParseMacAddressTable(macCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchMacTables[sw.Name] = macEntries;

//                            // ---- جمع‌آوری جدول LLDP ----
//                            List<LldpEntry> lldpEntries = new List<LldpEntry>();
//                            using (var client = new SshClient(sw.IP, sw.User, sw.Pass))
//                            {
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var lldpCmd = client.RunCommand("show lldp neighbors detail");
//                                await Task.Delay(200);
//                                lldpEntries = ParseLldpTable(lldpCmd.Result);

//                                client.Disconnect();
//                            }
//                            switchLldpTables[sw.Name] = lldpEntries;

//                            break; // موفقیت
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                switchLldpTables[sw.Name] = new List<LldpEntry>();
//                                break;
//                            }
//                            var backoff = (int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200);
//                            await Task.Delay(backoff);
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);



//            // ---- نگاشت MAC ها به سوئیچ و پورت واقعی ----
//            var results = new List<MappingResult>();

//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                // 1) MAC سیستم را پیدا کن
//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                // 2) پایه‌ها
//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.Vlan = found.Entry.Vlan;
//                r.FoundPort = found.Entry.Port;

//                // 3) همه MAC ها روی همان پورت
//                var allMacsSamePort = switchMacTables[found.Switch]
//                    .Where(m => m.Port == found.Entry.Port)
//                    .ToList();

//                // 4) تلفن را پیدا کن → VLAN = 30
//                var phone = allMacsSamePort.FirstOrDefault(m => m.Vlan == "30");

//                if (phone != null)
//                {
//                    r.PhoneMac = phone.Mac;
//                    r.PhoneVlan = phone.Vlan;
//                }

//                // 5) LLDP اگر موجود بود، پورت واقعی را تایید کن
//                var lldp = switchLldpTables[found.Switch]
//                    .FirstOrDefault(l => l.LocalPort == found.Entry.Port);

//                if (lldp != null)
//                    r.FoundPort = lldp.LocalPort;

//                results.Add(r);
//            }

//            return results;

//        }

//        // ---- Helpers ----
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(), @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");
//                if (m.Success)
//                {
//                    if (m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                        continue;

//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }

//        private static List<LldpEntry> ParseLldpTable(string output)
//        {
//            var list = new List<LldpEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            LldpEntry current = null;
//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();

//                if (trimmed.StartsWith("Local Port:"))
//                {
//                    current = new LldpEntry
//                    {
//                        LocalPort = trimmed.Substring("Local Port:".Length).Trim()
//                    };
//                }
//                else if (current != null)
//                {
//                    if (trimmed.StartsWith("Chassis id:"))
//                    {
//                        current.NeighborMac = NormalizeMac(trimmed.Substring("Chassis id:".Length).Trim());
//                    }
//                    else if (trimmed.StartsWith("System Name:"))
//                    {
//                        current.NeighborDevice = trimmed.Substring("System Name:".Length).Trim();
//                        list.Add(current);
//                        current = null;
//                    }
//                }
//            }

//            return list;
//        }
//    }
//}





















//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class MappingResult
//    {
//        public string Mac;
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;
//        public string Status; // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 4;
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 200;
//        public int CommandSpacingMs { get; set; } = 150;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        // ---- تابع اصلی ----
//        public static async Task<List<MappingResult>> MapMacsOnAccessSwitchesAsync(List<string> macs)
//        {
//            var options = new NetworkMapperOptions();

//            // ایجاد لیست switchهای access بر اساس IP رنج داده شده
//            var switchCfgs = new List<SwitchCfg>();
//            for (int i = 24; i <= 24; i++)
//            {
//                string ip = $"192.168.254.{i}";
//                switchCfgs.Add(new SwitchCfg(ip, $"SW{i}", "infosw", "Ii123456!"));
//            }

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            // جمع‌آوری جدول MAC از همه switchهای access
//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                            client.Connect();

//                            //client.RunCommand("terminal length 0");
//                            //await Task.Delay(200);

//                            var macCmd = client.RunCommand("show mac address-table");
//                            await Task.Delay(200);


//                            switchMacTables[sw.Name] = ParseMacAddressTable(macCmd.Result);

//                            client.Disconnect();
//                            break;
//                        }
//                        catch(Exception exp)
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                break;
//                            }
//                            var backoff = (int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200);
//                            await Task.Delay(backoff);
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // نگاشت MACها به switch و پورت
//            var results = new List<MappingResult>();
//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                }
//                else
//                {
//                    r.Status = "FOUND";
//                    r.FoundSwitch = found.Switch;
//                    r.FoundPort = found.Entry.Port;
//                    r.Vlan = found.Entry.Vlan;
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        // ---- Helpers ----
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;

//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                // Regex جدید: VLAN = عدد یا All، MAC = xxxx.xxxx.xxxx، Type = هر چیزی، Port = آخرین ستون
//                var m = Regex.Match(line.Trim(), @"^(?<vlan>\d+|All)\s+(?<mac>[0-9a-fA-F]{4}\.[0-9a-fA-F]{4}\.[0-9a-fA-F]{4})\s+\S+\s+(?<port>\S+)$");
//                if (m.Success)
//                {
//                    // اگر پورت CPU است، نادیده می‌گیریم
//                    if (m.Groups["port"].Value.Equals("CPU", StringComparison.OrdinalIgnoreCase))
//                        continue;

//                    list.Add(new MacEntry
//                    {
//                        Vlan = m.Groups["vlan"].Value,
//                        Mac = NormalizeMac(m.Groups["mac"].Value),
//                        Port = m.Groups["port"].Value
//                    });
//                }
//            }
//            return list;
//        }

//    }
//}















//using Renci.SshNet;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace MyNetworkLib
//{
//    public record SwitchCfg(string IP, string Name, string User, string Pass);

//    public class MacEntry
//    {
//        public string Vlan;
//        public string Mac;
//        public string Port;
//    }

//    public class CdpNeighbor
//    {
//        public string LocalInterface;
//        public string DeviceId;
//        public string PortId;
//        public string ManagementAddress;
//    }

//    public class MappingResult
//    {
//        public string Mac;
//        public string FoundSwitch;
//        public string FoundPort;
//        public string Vlan;
//        public List<(string SwitchName, string SwitchIP, string Port, string RemoteDevice, string RemotePort)> Path = new();
//        public string Notes;
//        public string Status; // FOUND / NOT FOUND / ERROR
//    }

//    public class NetworkMapperOptions
//    {
//        public int MaxParallelSsh { get; set; } = 4;
//        public int SshTimeoutSeconds { get; set; } = 10;
//        public int DelayBetweenSwitchMs { get; set; } = 200;
//        public int CommandSpacingMs { get; set; } = 150;
//        public double UseFilteredMacQueriesThreshold { get; set; } = 0.2;
//        public int MaxRetries { get; set; } = 3;
//    }

//    public static class NetworkMapper
//    {
//        public static async Task<List<MappingResult>> MapMacsAsync(List<string> macs, List<SwitchCfg> switchCfgs, NetworkMapperOptions options = null)
//        {
//            options ??= new NetworkMapperOptions();
//            int switchCount = switchCfgs.Count;
//            int macCount = macs.Count;
//            bool useFilterPerMac = (double)macCount / Math.Max(1, switchCount) < options.UseFilteredMacQueriesThreshold;

//            var switchMacTables = new ConcurrentDictionary<string, List<MacEntry>>();
//            var switchCdpNeighbors = new ConcurrentDictionary<string, List<CdpNeighbor>>();
//            var nameToIp = switchCfgs.ToDictionary(s => s.Name, s => s.IP, StringComparer.OrdinalIgnoreCase);

//            var semaphore = new SemaphoreSlim(options.MaxParallelSsh);

//            // ----- جمع‌آوری اطلاعات سوییچ‌ها -----
//            var tasks = switchCfgs.Select(async sw =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    int attempt = 0;
//                    while (true)
//                    {
//                        attempt++;
//                        try
//                        {
//                            using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                            client.Connect();

//                            if (!useFilterPerMac)
//                            {
//                                var macCmd = client.RunCommand("terminal length 0 ; show mac address-table");
//                                await Task.Delay(options.CommandSpacingMs);
//                                switchMacTables[sw.Name] = ParseMacAddressTable(macCmd.Result);
//                            }

//                            var cdpCmd = client.RunCommand("terminal length 0 ; show cdp neighbors detail");
//                            await Task.Delay(options.CommandSpacingMs);
//                            switchCdpNeighbors[sw.Name] = ParseCdpNeighborsDetail(cdpCmd.Result);

//                            client.Disconnect();
//                            break;
//                        }
//                        catch
//                        {
//                            if (attempt >= options.MaxRetries)
//                            {
//                                switchMacTables[sw.Name] = new List<MacEntry>();
//                                switchCdpNeighbors[sw.Name] = new List<CdpNeighbor>();
//                                break;
//                            }
//                            var backoff = (int)(Math.Pow(2, attempt) * 200) + new Random().Next(0, 200);
//                            await Task.Delay(backoff);
//                        }
//                    }
//                }
//                finally
//                {
//                    await Task.Delay(options.DelayBetweenSwitchMs);
//                    semaphore.Release();
//                }
//            }).ToList();

//            await Task.WhenAll(tasks);

//            // ----- اگر adaptive فعال بود، برای هر MAC روی هر سوئیچ query فیلتر شده اجرا شود -----
//            if (useFilterPerMac)
//            {
//                var perMacSemaphore = new SemaphoreSlim(options.MaxParallelSsh);
//                var perMacTasks = new List<Task>();
//                foreach (var sw in switchCfgs)
//                {
//                    switchMacTables[sw.Name] = new List<MacEntry>();
//                    foreach (var mac in macs)
//                    {
//                        perMacTasks.Add(Task.Run(async () =>
//                        {
//                            await perMacSemaphore.WaitAsync();
//                            try
//                            {
//                                using var client = new SshClient(sw.IP, sw.User, sw.Pass);
//                                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(options.SshTimeoutSeconds);
//                                client.Connect();

//                                var cmd = client.RunCommand($"terminal length 0 ; show mac address-table address {FormatMacForCisco(mac)}");
//                                await Task.Delay(options.CommandSpacingMs);
//                                var entries = ParseMacAddressTable(cmd.Result);
//                                if (entries.Any())
//                                {
//                                    var list = switchMacTables.GetOrAdd(sw.Name, _ => new List<MacEntry>());
//                                    lock (list) list.AddRange(entries);
//                                }

//                                client.Disconnect();
//                            }
//                            catch { }
//                            finally { perMacSemaphore.Release(); }
//                        }));
//                    }
//                }
//                await Task.WhenAll(perMacTasks);
//            }

//            // ----- Mapping MAC ها -----
//            var results = new List<MappingResult>();

//            foreach (var macRaw in macs)
//            {
//                var mac = NormalizeMac(macRaw);
//                var r = new MappingResult { Mac = mac, Status = "ERROR" };

//                var found = switchMacTables
//                    .SelectMany(kv => kv.Value.Select(e => new { Switch = kv.Key, Entry = e }))
//                    .FirstOrDefault(x => NormalizeMac(x.Entry.Mac) == mac);

//                if (found == null)
//                {
//                    r.Status = "NOT FOUND";
//                    results.Add(r);
//                    continue;
//                }

//                r.Status = "FOUND";
//                r.FoundSwitch = found.Switch;
//                r.FoundPort = found.Entry.Port;
//                r.Vlan = found.Entry.Vlan;

//                var visitedSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                string currentSwitch = r.FoundSwitch;
//                string currentPort = r.FoundPort;

//                while (!string.IsNullOrEmpty(currentSwitch) && !visitedSwitches.Contains(currentSwitch))
//                {
//                    visitedSwitches.Add(currentSwitch);
//                    switchCdpNeighbors.TryGetValue(currentSwitch, out var neighs);
//                    neighs ??= new List<CdpNeighbor>();

//                    var next = neighs.FirstOrDefault(n => NormalizeInterface(n.LocalInterface).Contains(NormalizeInterface(currentPort)) ||
//                                                          NormalizeInterface(currentPort).Contains(NormalizeInterface(n.LocalInterface)));

//                    nameToIp.TryGetValue(currentSwitch, out var curIp);
//                    r.Path.Add((SwitchName: currentSwitch, SwitchIP: curIp ?? "", Port: currentPort, RemoteDevice: next?.DeviceId ?? "", RemotePort: next?.PortId ?? ""));

//                    if (next == null || string.IsNullOrEmpty(next.DeviceId)) break;

//                    var nextSwitchCfg = switchCfgs.FirstOrDefault(s => string.Equals(s.Name, next.DeviceId, StringComparison.OrdinalIgnoreCase));
//                    if (nextSwitchCfg != null)
//                    {
//                        currentSwitch = nextSwitchCfg.Name;
//                        currentPort = next.PortId;
//                        continue;
//                    }

//                    currentSwitch = null;
//                }

//                results.Add(r);
//            }

//            return results;
//        }

//        // ----- Helpers -----
//        private static string NormalizeMac(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return null;
//            var hex = Regex.Replace(raw.Trim(), @"[^0-9a-fA-F]", "");
//            return hex.Length == 12 ? hex.ToLower() : null;
//        }

//        private static string FormatMacForCisco(string mac)
//        {
//            var n = NormalizeMac(mac);
//            if (n == null) return mac;
//            return $"{n.Substring(0, 4)}.{n.Substring(4, 4)}.{n.Substring(8, 4)}";
//        }

//        private static List<MacEntry> ParseMacAddressTable(string output)
//        {
//            var list = new List<MacEntry>();
//            if (string.IsNullOrWhiteSpace(output)) return list;
//            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (var line in lines)
//            {
//                var m = Regex.Match(line.Trim(), @"^(\d+)\s+([0-9a-fA-F\.\-:]{6,})\s+\S+\s+(\S+)$");
//                if (m.Success)
//                    list.Add(new MacEntry { Vlan = m.Groups[1].Value, Mac = NormalizeMac(m.Groups[2].Value) ?? m.Groups[2].Value, Port = m.Groups[3].Value });
//            }
//            return list;
//        }

//        private static List<CdpNeighbor> ParseCdpNeighborsDetail(string output)
//        {
//            var res = new List<CdpNeighbor>();
//            if (string.IsNullOrWhiteSpace(output)) return res;
//            var blocks = Regex.Split(output, @"\r?\n\s*\r?\n").Select(b => b.Trim()).Where(b => b.Length > 0);
//            foreach (var block in blocks)
//            {
//                var devMatch = Regex.Match(block, @"Device ID:\s*(.+)");
//                var intfMatch = Regex.Match(block, @"Interface:\s*(.+?),\s*Port ID.*:\s*(.+)");
//                var mgmtMatch = Regex.Match(block, @"Management address:\s*(\S+)");

//                if (devMatch.Success && intfMatch.Success)
//                {
//                    res.Add(new CdpNeighbor
//                    {
//                        DeviceId = devMatch.Groups[1].Value.Trim(),
//                        LocalInterface = intfMatch.Groups[1].Value.Trim(),
//                        PortId = intfMatch.Groups[2].Value.Trim(),
//                        ManagementAddress = mgmtMatch.Success ? mgmtMatch.Groups[1].Value.Trim() : ""
//                    });
//                }
//            }
//            return res;
//        }

//        private static string NormalizeInterface(string s)
//        {
//            if (string.IsNullOrEmpty(s)) return "";
//            s = s.Replace("GigabitEthernet", "Gi").Replace("FastEthernet", "Fa").Replace("TenGigabitEthernet", "Te");
//            s = s.Replace(" ", "").Replace("/", "").Replace(".", "").ToLower();
//            return s;
//        }
//    }
//}