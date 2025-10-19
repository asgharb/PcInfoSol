using Hardware.Info;
using SqlDataExtention.Entity;
using System.Linq;

namespace PcInfoWin.Provider
{
    public class CpuInfoProvider
    {
        private readonly HardwareInfo _hardwareInfo;
        public CpuInfoProvider()
        {
            _hardwareInfo = new HardwareInfo();
            _hardwareInfo.RefreshCPUList();
        }
        public CpuInfo GetCpuInfo()
        {
            var cpu = _hardwareInfo.CpuList.FirstOrDefault();
            if (cpu == null) return null;
            return new CpuInfo
            {
                Name = cpu.Name,
                Manufacturer = cpu.Manufacturer,
            };
        }
    }
}
