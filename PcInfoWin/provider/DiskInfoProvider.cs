using Microsoft.Win32.SafeHandles;
using PcInfoWin.Entity.Models;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;

namespace PcInfoWin.Provider
{
    public class DiskInfoProvider
    { // Win32 API constants
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const uint OPEN_EXISTING = 3;
        private const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400;

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        struct STORAGE_PROPERTY_QUERY
        {
            public int PropertyId;
            public int QueryType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] AdditionalParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct STORAGE_DEVICE_DESCRIPTOR
        {
            public uint Version;
            public uint Size;
            public byte DeviceType;
            public byte DeviceTypeModifier;
            [MarshalAs(UnmanagedType.U1)]
            public bool RemovableMedia;
            [MarshalAs(UnmanagedType.U1)]
            public bool CommandQueueing;
            public uint VendorIdOffset;
            public uint ProductIdOffset;
            public uint ProductRevisionOffset;
            public uint SerialNumberOffset;
            public byte BusType;
            public uint RawPropertiesLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] RawDeviceProperties;
        }

        // PInvoke
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            ref STORAGE_PROPERTY_QUERY lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        // Public method
        public List<DiskInfo> GetDisks()
        {
            var list = new List<DiskInfo>();

            for (int i = 0; i < 16; i++) // بررسی دیسک‌های 0 تا 15
            {
                string path = $"\\\\.\\PhysicalDrive{i}";
                try
                {
                    using (var handle = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid) continue;

                        STORAGE_PROPERTY_QUERY query = new STORAGE_PROPERTY_QUERY
                        {
                            PropertyId = 0, // StorageDeviceProperty
                            QueryType = 0,
                            AdditionalParameters = new byte[1]
                        };

                        uint outSize = 1024;
                        IntPtr outBuffer = Marshal.AllocHGlobal((int)outSize);

                        bool success = DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, (uint)Marshal.SizeOf(query),
                            outBuffer, outSize, out uint bytesReturned, IntPtr.Zero);

                        if (!success)
                        {
                            Marshal.FreeHGlobal(outBuffer);
                            continue;
                        }

                        STORAGE_DEVICE_DESCRIPTOR desc = Marshal.PtrToStructure<STORAGE_DEVICE_DESCRIPTOR>(outBuffer);
                        Marshal.FreeHGlobal(outBuffer);

                        string model = ReadStringFromOffset(outBuffer, desc.ProductIdOffset);
                        string manufacturer = ReadStringFromOffset(outBuffer, desc.VendorIdOffset);
                        string type = GetDeviceType(desc.BusType);

                        // ظرفیت با WMI fallback
                        int sizeGB = GetDiskSizeGB(i);

                        list.Add(new DiskInfo
                        {
                            Model = model.Trim(),
                            Manufacturer = manufacturer.Trim(),
                            SizeGB = sizeGB,
                            Type = type
                        });
                    }
                }
                catch { /* نادیده گرفتن دیسک‌های ناموجود */ }
            }

            return list;
        }

        private string ReadStringFromOffset(IntPtr buffer, uint offset)
        {
            if (offset == 0) return "Unknown";
            IntPtr ptr = IntPtr.Add(buffer, (int)offset);
            return Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
        }

        private string GetDeviceType(byte busType)
        {
            switch (busType)
            {
                case 0x0: return "Unknown";
                case 0x1: return "SCSI";
                case 0x2: return "ATAPI";
                case 0x3: return "ATA";
                case 0x4: return "IEEE1394";
                case 0x5: return "SSA";
                case 0x6: return "Fibre";
                case 0x7: return "USB";
                case 0x8: return "RAID";
                case 0x9: return "iSCSI";
                case 0xA: return "SAS";
                case 0xB: return "SATA";
                case 0xC: return "SD";
                case 0xD: return "MMC";
                case 0xE: return "Virtual";
                case 0xF: return "FileBackedVirtual";
                case 0x10: return "Spaces";
                case 0x11: return "NVMe";
                default: return "Unknown";
            }
        }


        private int GetDiskSizeGB(int diskIndex)
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT Size FROM Win32_DiskDrive WHERE Index={diskIndex}");
                foreach (var mo in searcher.Get())
                {
                    if (mo["Size"] != null && ulong.TryParse(mo["Size"].ToString(), out ulong sizeBytes))
                    {
                        return (int)(sizeBytes / (1024 * 1024 * 1024));
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

    }
}
