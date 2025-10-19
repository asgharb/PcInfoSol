using Hardware.Info;
using SqlDataExtention.Entity;
using System.Linq;


namespace PcInfoWin.Provider
{
    public class GpuInfoProvider
    {
        private readonly HardwareInfo _hardwareInfo;

        public GpuInfoProvider()
        {
            _hardwareInfo = new HardwareInfo();
            _hardwareInfo.RefreshVideoControllerList(); 
        }

        public GpuInfo GetGpuInfo()
        {
            var gpu = _hardwareInfo.VideoControllerList.FirstOrDefault();
            if (gpu == null) return null;

            return new GpuInfo
            {
                Name = gpu.Name,
                Manufacturer = gpu.Manufacturer,
                VideoProcessor = gpu.VideoProcessor,
                DriverVersion = gpu.DriverVersion,
                MemorySizeGB = (int)(gpu.AdapterRAM / (1024 * 1024 * 1024))
            };
        }
    }
}