using Renci.SshNet;
using System.Text.RegularExpressions;

namespace MyNetworkLib
{
    public class MacInfo
    {
        public string Mac { get; set; }
        public string Port { get; set; }
        public string Type { get; set; }  // Phone / PC / Unknown
    }

    public class Options
    {
        public int ShellReadIntervalMs { get; set; } = 120;
        public int ShellReadTimeoutMs { get; set; } = 1500;
    }

    public class CiscoMacScanner
    {
        // -------------------- MAIN METHOD --------------------
        public List<MacInfo> GetMacPortInfo(string ip, Options opt = null)
        {
            string user = "infosw";
            string pass = "Ii123456!";
            opt ??= new Options();

            using var client = new SshClient(ip, user, pass);
            client.Connect();

            using var stream = client.CreateShellStream("vt100", 80, 24, 800, 600, 4096);

            ClearShellBuffer(stream);
            WriteAndRead(stream, "terminal length 0", opt);
            ClearShellBuffer(stream);

            // 1) EXTRACT TRUNK PORTS
            string statusOut = WriteAndRead(stream, "show interfaces status", opt);
            ClearShellBuffer(stream);

            var trunkPorts = ExtractTrunkPorts(statusOut);

            // 2) READ MAC TABLE
            string macOut = WriteAndRead(stream, "show mac address-table", opt);
            ClearShellBuffer(stream);

            return ParseMacTable(macOut, trunkPorts);
        }

        // -------------------- READ METHODS --------------------
        private string WriteAndRead(ShellStream stream, string cmd, Options opt)
        {
            stream.WriteLine(cmd);
            Thread.Sleep(opt.ShellReadIntervalMs);

            string data = "";
            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < opt.ShellReadTimeoutMs)
            {
                if (stream.DataAvailable)
                {
                    data += stream.Read();
                    Thread.Sleep(opt.ShellReadIntervalMs);
                }
                else
                {
                    Thread.Sleep(20);
                }
            }

            return data;
        }

        private void ClearShellBuffer(ShellStream stream)
        {
            while (stream.DataAvailable)
            {
                stream.Read();
                Thread.Sleep(15);
            }
        }

        // -------------------- PARSERS --------------------
        private HashSet<string> ExtractTrunkPorts(string output)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var lines = output.Split('\n');

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (!(line.StartsWith("Gi") || line.StartsWith("Fa")))
                    continue;

                var firstSpace = line.IndexOf(' ');
                if (firstSpace < 0) continue;

                string port = line.Substring(0, firstSpace).Trim();

                if (Regex.IsMatch(line, @"\btrunk\b", RegexOptions.IgnoreCase))
                    set.Add(port);
            }

            return set;
        }

        private List<MacInfo> ParseMacTable(string output, HashSet<string> trunkPorts)
        {
            var list = new List<MacInfo>();
            var seen = new HashSet<string>();

            var lines = output.Split('\n');

            foreach (var ln in lines)
            {
                var l = ln.Trim();
                if (!l.Contains("DYNAMIC", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = Regex.Split(l, @"\s+");
                if (parts.Length < 4)
                    continue;

                string mac = parts[1];
                string port = parts[3];

                if (trunkPorts.Contains(port))
                    continue;

                string key = mac + "|" + port;
                if (seen.Contains(key))
                    continue;

                seen.Add(key);

                list.Add(new MacInfo
                {
                    Mac = mac,
                    Port = port,
                    Type = DetectType(mac)
                });
            }

            return list;
        }

        // -------------------- PHONE/PC DETECTOR --------------------
        private string DetectType(string mac)
        {
            string cleanMac = mac.Replace(".", "").Replace(":", "").ToUpper();

            if (cleanMac.Length < 6)
                return "Unknown";

            string oui = cleanMac.Substring(0, 6);

            if (PhoneOUI.Contains(oui))
                return "Phone";

            if (PcOUI.Contains(oui))
                return "PC";

            return "Unknown";
        }

        // -------------------- OUI DATABASE --------------------
        private static readonly HashSet<string> PhoneOUI = new HashSet<string>
        {
            "001B54", "002333", "002584", "003080", "001EF7", "001906", // Cisco
            "001565", "805EC0", "34800D",                               // Yealink
            "000B82", "000E08",                                         // Grandstream
            "000413", "0004F2",                                         // Polycom
        };



        private static readonly HashSet<string> PcOUI = new HashSet<string>
{
    "74563C", "60CF84", "E89C25", "A85E45",
    "242FD0", "581122", "9C5322", "04D4C4",
    "98E743", "24418C", "0C9D92", "04D9F5",
    "4437E6", "2CFDA1", "386B1C", "08BFB8",
    "DC4628", "B42E99", "D85ED3", "00E0FF",
    "C4C6E6", "C03532", "50EBF6", "745D22",
    "107B44", "00FF2D", "7C10C9", "C8F750",
    "FC7774", "F0B61E", "700894", "381428",
    "94E23C", "546CEB", "089204", "D4D853",
    "AC91A1", "FE0558", "482AE3", "1C1B0D",
    "6083E7", "706894", "502B73", "9C5C8E"
};

    }
}
