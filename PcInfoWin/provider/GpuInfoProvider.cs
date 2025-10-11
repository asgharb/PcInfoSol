using Hardware.Info;
using PcInfoWin.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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