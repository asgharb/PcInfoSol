using SqlDataExtention.Entity;
using System.Linq;
using System.Management;


namespace PcInfoWin.Provider
{
    public class MotherboardInfoProvider
    {
        public MotherboardInfo GetMotherboardInfo()
        {
            var mb = new MotherboardInfo();

            // اطلاعات مادر برد
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
            {
                var board = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (board != null)
                {
                    mb.Manufacturer = board["Manufacturer"]?.ToString() ?? "Unknown";
                    mb.Product = board["Product"]?.ToString() ?? "Unknown";
                    mb.SerialNumber = board["SerialNumber"]?.ToString() ?? "Unknown";
                }
            }

            // تعداد کل اسلات‌های RAM
            int totalSlots = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemoryArray"))
            {
                foreach (ManagementObject array in searcher.Get())
                {
                    totalSlots += (ushort)(array["MemoryDevices"] ?? 0);
                }
            }
            mb.TotalRamSlots = totalSlots;

            // تعداد اسلات‌های پر شده
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
            {
                mb.UsedRamSlots = searcher.Get().Count;
            }

            return mb;
        }
    }
}
