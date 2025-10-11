using PcInfoWin.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace PcInfoWin.Provider
{
    public class MonitorInfoProvider
    {
        public List<MonitorInfo> GetAllMonitors()
        {
            var screens = Screen.AllScreens;
            var results = new List<MonitorInfo>();

            // فقط مانیتورهای فعال را اضافه کن
            foreach (var s in screens.Where(sc => sc.Primary || sc.Bounds.Width > 0))
            {
                results.Add(new MonitorInfo
                {
                    DeviceName = s.DeviceName,
                    Width = s.Bounds.Width,
                    Height = s.Bounds.Height
                });
            }

            // 2) Query WMI WmiMonitorID (names/manufacturer/serial)
            Dictionary<string, MonitorInfo> byInstance = new Dictionary<string, MonitorInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorID"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        try
                        {
                            string instanceName = (mo["InstanceName"] as string) ?? "";
                            string manufacturer = ParseWmiStringArray(mo["ManufacturerName"] as ushort[]);
                            string userName = ParseWmiStringArray(mo["UserFriendlyName"] as ushort[]);
                            string serial = ParseWmiStringArray(mo["SerialNumberID"] as ushort[]);
                            string productId = ParseWmiStringArray(mo["ProductCodeID"] as ushort[]);

                            var info = new MonitorInfo
                            {
                                InstanceName = instanceName,
                                Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                                UserFriendlyName = string.IsNullOrWhiteSpace(userName) ? null : userName,
                                ProductCodeID = string.IsNullOrWhiteSpace(productId) ? null : productId,
                                SerialNumber = string.IsNullOrWhiteSpace(serial) ? null : serial
                            };

                            byInstance[instanceName] = info;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 3) Query WmiMonitorBasicDisplayParams for physical dims (mm)
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBasicDisplayParams"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        try
                        {
                            string instanceName = (mo["InstanceName"] as string) ?? "";
                            int maxH = ConvertToIntSafe(mo["MaxHorizontalImageSize"]);
                            int maxV = ConvertToIntSafe(mo["MaxVerticalImageSize"]);

                            if (byInstance.TryGetValue(instanceName, out var info))
                            {
                                info.PhysicalWidthMm = maxH;
                                info.PhysicalHeightMm = maxV;
                            }
                            else
                            {
                                byInstance[instanceName] = new MonitorInfo
                                {
                                    InstanceName = instanceName,
                                    PhysicalWidthMm = maxH,
                                    PhysicalHeightMm = maxV
                                };
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 4) Correlate Screen entries with WMI instances
            var unmatchedWmi = new List<MonitorInfo>(byInstance.Values);

            for (int i = 0; i < results.Count; i++)
            {
                var scrInfo = results[i];
                var screen = screens.FirstOrDefault(s => s.DeviceName == scrInfo.DeviceName);

                MonitorInfo matched = null;

                if (unmatchedWmi.Count > 0)
                {
                    foreach (var w in unmatchedWmi)
                    {
                        if (!string.IsNullOrEmpty(w.InstanceName) && !string.IsNullOrEmpty(scrInfo.DeviceName) &&
                            w.InstanceName.IndexOf(scrInfo.DeviceName.Trim('\\', '.'), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matched = w;
                            break;
                        }
                    }

                    // fallback: position-based match
                    if (matched == null && unmatchedWmi.Count == results.Count)
                        matched = unmatchedWmi[i];

                    if (matched != null)
                    {
                        scrInfo.InstanceName = scrInfo.InstanceName ?? matched.InstanceName;
                        scrInfo.UserFriendlyName = scrInfo.UserFriendlyName ?? matched.UserFriendlyName;
                        scrInfo.Manufacturer = scrInfo.Manufacturer ?? matched.Manufacturer;
                        scrInfo.ProductCodeID = scrInfo.ProductCodeID ?? matched.ProductCodeID;
                        scrInfo.SerialNumber = scrInfo.SerialNumber ?? matched.SerialNumber;
                        scrInfo.PhysicalWidthMm = scrInfo.PhysicalWidthMm == 0 ? matched.PhysicalWidthMm : scrInfo.PhysicalWidthMm;
                        scrInfo.PhysicalHeightMm = scrInfo.PhysicalHeightMm == 0 ? matched.PhysicalHeightMm : scrInfo.PhysicalHeightMm;

                        unmatchedWmi.Remove(matched);
                    }
                }

                // محاسبه دقیق SizeInInches با DPI واقعی اگر Screen فعال باشد
                if (screen != null)
                {
                    scrInfo.SizeInInches = MonitorHelper.GetMonitorDiagonalInches(screen);
                }
            }

            return results;
        }

        private static int ConvertToIntSafe(object o)
        {
            try { return (o != null) ? Convert.ToInt32(o) : 0; }
            catch { return 0; }
        }

        private static string ParseWmiStringArray(object raw)
        {
            if (raw == null) return null;

            if (raw is ushort[] arr)
            {
                var chars = arr.TakeWhile(c => c != 0).Select(c => (char)c).ToArray();
                return new string(chars).Trim();
            }

            if (raw is byte[] barr)
            {
                return System.Text.Encoding.UTF8.GetString(barr).Trim('\0').Trim();
            }

            if (raw is string sraw)
                return sraw.Trim();

            return null;
        }
    }
}



















//using PcInfoWin.Entity.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Management;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace PcInfoWin.Provider
//{
//    public class MonitorInfoProvider
//    {
//        public  List<MonitorInfo> GetAllMonitors()
//        {
//            var screens = Screen.AllScreens;
//            var results = new List<MonitorInfo>();

//            // 1) Create MonitorInfo objects from Screen (guaranteed)
//            foreach (var s in screens)
//            {
//                results.Add(new MonitorInfo
//                {
//                    DeviceName = s.DeviceName, // like \\.\DISPLAY1
//                    Width = s.Bounds.Width,
//                    Height = s.Bounds.Height
//                });
//            }

//            // 2) Query WMI WmiMonitorID (names/manufacturer/serial)
//            Dictionary<string, MonitorInfo> byInstance = new Dictionary<string, MonitorInfo>(StringComparer.OrdinalIgnoreCase);
//            try
//            {
//                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorID"))
//                {
//                    foreach (ManagementObject mo in searcher.Get())
//                    {
//                        try
//                        {
//                            string instanceName = (mo["InstanceName"] as string) ?? "";
//                            string manufacturer = ParseWmiStringArray(mo["ManufacturerName"] as ushort[]);
//                            string userName = ParseWmiStringArray(mo["UserFriendlyName"] as ushort[]);
//                            string serial = ParseWmiStringArray(mo["SerialNumberID"] as ushort[]);
//                            string productId = ParseWmiStringArray(mo["ProductCodeID"] as ushort[]);

//                            var info = new MonitorInfo
//                            {
//                                //InstanceName = instanceName,
//                                Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
//                                UserFriendlyName = string.IsNullOrWhiteSpace(userName) ? null : userName,
//                                ProductCodeID = string.IsNullOrWhiteSpace(productId) ? null : productId,
//                                SerialNumber = string.IsNullOrWhiteSpace(serial) ? null : serial
//                            };

//                            byInstance[instanceName] = info;
//                        }
//                        catch
//                        {
//                            // ignore per-monitor parse errors
//                        }
//                    }
//                }
//            }
//            catch (ManagementException mex)
//            {
//                // WMI may fail on some systems — don't crash; just continue
//                Console.Error.WriteLine("WMI WmiMonitorID query failed: " + mex.Message);
//            }
//            catch (Exception ex)
//            {
//                Console.Error.WriteLine("WMI WmiMonitorID unexpected error: " + ex.Message);
//            }

//            // 3) Query WmiMonitorBasicDisplayParams for physical dims (mm)
//            try
//            {
//                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBasicDisplayParams"))
//                {
//                    foreach (ManagementObject mo in searcher.Get())
//                    {
//                        try
//                        {
//                            string instanceName = (mo["InstanceName"] as string) ?? "";
//                            // fields: MaxHorizontalImageSize, MaxVerticalImageSize are in millimeters
//                            int maxH = ConvertToIntSafe(mo["MaxHorizontalImageSize"]);
//                            int maxV = ConvertToIntSafe(mo["MaxVerticalImageSize"]);

//                            if (byInstance.TryGetValue(instanceName, out var info))
//                            {
//                                info.PhysicalWidthMm = maxH;
//                                info.PhysicalHeightMm = maxV;
//                            }
//                            else
//                            {
//                                var newInfo = new MonitorInfo
//                                {
//                                    InstanceName = instanceName,
//                                    PhysicalWidthMm = maxH,
//                                    PhysicalHeightMm = maxV
//                                };
//                                byInstance[instanceName] = newInfo;
//                            }
//                        }
//                        catch
//                        {
//                            // ignore single monitor errors
//                        }
//                    }
//                }
//            }
//            catch (ManagementException mex)
//            {
//                Console.Error.WriteLine("WMI WmiMonitorBasicDisplayParams query failed: " + mex.Message);
//            }
//            catch (Exception ex)
//            {
//                Console.Error.WriteLine("WMI WmiMonitorBasicDisplayParams unexpected error: " + ex.Message);
//            }

//            // 4) Try to correlate Screen entries with WMI instances.
//            // Matching heuristic: InstanceName often contains a display VID/PID and sometimes the display index.
//            // We'll attempt simple matching by searching for VID/PID fragments in DeviceName or by order if necessary.

//            // Build a list of unmatched WMI infos
//            var unmatchedWmi = new List<MonitorInfo>(byInstance.Values);

//            foreach (var scrInfo in results)
//            {
//                // try to find WMI entry that contains the display name parts
//                MonitorInfo matched = null;

//                // heuristic 1: instance name may contain "DISPLAY" or the adapter/port name; try index-based match
//                // If there are equal counts, map by index
//                // We'll attempt finding by order: if counts match, map by index
//                // first pass: try find by VID/PID substring
//                if (unmatchedWmi.Count > 0)
//                {
//                    foreach (var w in unmatchedWmi)
//                    {
//                        if (w.InstanceName != null && scrInfo.DeviceName != null &&
//                            w.InstanceName.IndexOf("DISPLAY", StringComparison.OrdinalIgnoreCase) >= 0 &&
//                            w.InstanceName.IndexOf(scrInfo.DeviceName.Trim('\\', '.'), StringComparison.OrdinalIgnoreCase) >= 0)
//                        {
//                            matched = w;
//                            break;
//                        }

//                        // also try matching by numeric index (DISPLAY1 -> 1)
//                        try
//                        {
//                            if (scrInfo.DeviceName != null && scrInfo.DeviceName.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase) && w.InstanceName != null)
//                            {
//                                // if instance contains "DISPLAY1" etc
//                                if (w.InstanceName.IndexOf(scrInfo.DeviceName.Replace(@"\\.\", ""), StringComparison.OrdinalIgnoreCase) >= 0)
//                                {
//                                    matched = w;
//                                    break;
//                                }
//                            }
//                        }
//                        catch { }
//                    }

//                    // fallback: if still not matched and counts equal, match by position
//                    if (matched == null && unmatchedWmi.Count == results.Count)
//                    {
//                        int pos = results.IndexOf(scrInfo);
//                        if (pos >= 0 && pos < unmatchedWmi.Count)
//                            matched = unmatchedWmi[pos];
//                    }

//                    if (matched != null)
//                    {
//                        // merge info
//                        scrInfo.InstanceName = scrInfo.InstanceName ?? matched.InstanceName;
//                        scrInfo.UserFriendlyName = scrInfo.UserFriendlyName ?? matched.UserFriendlyName;
//                        scrInfo.Manufacturer = scrInfo.Manufacturer ?? matched.Manufacturer;
//                        scrInfo.ProductCodeID = scrInfo.ProductCodeID ?? matched.ProductCodeID;
//                        scrInfo.SerialNumber = scrInfo.SerialNumber ?? matched.SerialNumber;
//                        scrInfo.PhysicalWidthMm = scrInfo.PhysicalWidthMm == 0 ? matched.PhysicalWidthMm : scrInfo.PhysicalWidthMm;
//                        scrInfo.PhysicalHeightMm = scrInfo.PhysicalHeightMm == 0 ? matched.PhysicalHeightMm : scrInfo.PhysicalHeightMm;
//                        //if (scrInfo.PhysicalWidthMm > 0 && scrInfo.PhysicalHeightMm > 0)
//                        //{
//                        //    double widthInch = scrInfo.PhysicalWidthMm / 25.4;
//                        //    double heightInch = scrInfo.PhysicalHeightMm / 25.4;
//                        //    scrInfo.SizeInInches= Math.Sqrt(widthInch * widthInch + heightInch * heightInch);
//                        //}

//                        unmatchedWmi.Remove(matched);
//                    }
//                }
//            }

//            // 5) Any leftover WMI-only monitors (not mapped to a Screen) — add them as separate entries
//            foreach (var leftover in unmatchedWmi)
//            {
//                // give at least resolution ? as unknown
//                results.Add(leftover);
//            }

//            return results;
//        }

//        private static int ConvertToIntSafe(object o)
//        {
//            try
//            {
//                if (o == null) return 0;
//                return Convert.ToInt32(o);
//            }
//            catch { return 0; }
//        }

//        // WmiMonitorID returns ushort[] where each ushort is a Unicode char code (or 0)
//        // Older code sometimes gives byte[]; we handle both.
//        private static string ParseWmiStringArray(object raw)
//        {
//            try
//            {
//                if (raw == null) return null;

//                if (raw is ushort[] arr)
//                {
//                    var chars = arr.TakeWhile(c => c != 0).Select(c => (char)c).ToArray();
//                    return new string(chars).Trim();
//                }

//                if (raw is byte[] barr)
//                {
//                    // sometimes bytes are provided; interpret as ASCII/UTF8
//                    var s = System.Text.Encoding.UTF8.GetString(barr).Trim('\0').Trim();
//                    return s;
//                }

//                if (raw is string sraw)
//                {
//                    return sraw.Trim();
//                }

//                return null;
//            }
//            catch
//            {
//                return null;
//            }
//        }
//    }
//}