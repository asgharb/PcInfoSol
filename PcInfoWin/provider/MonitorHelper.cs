using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PcInfoWin.Provider
{
    public static class MonitorHelper
    {
        // PInvoke برای گرفتن DPI واقعی مانیتور
        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        private enum Monitor_DPI_Type
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        // تابع کمکی برای DPI
        public static (double dpiX, double dpiY) GetMonitorDpi(Screen screen)
        {
            var pt = new POINT { X = screen.Bounds.Left + 1, Y = screen.Bounds.Top + 1 };
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            if (hMonitor != IntPtr.Zero)
            {
                if (GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Effective_DPI, out uint dpiX, out uint dpiY) == 0)
                {
                    return (dpiX, dpiY);
                }
            }

            // fallback: از دسکتاپ
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (g.DpiX, g.DpiY);
            }
        }



        public static double GetMonitorDiagonalInches(Screen screen)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX;
                float dpiY = g.DpiY;

                double widthInch = screen.Bounds.Width / dpiX;
                double heightInch = screen.Bounds.Height / dpiY;

                double diagonal = Math.Sqrt(widthInch * widthInch + heightInch * heightInch);

                return Math.Round(diagonal, 2); // محدود به 2 رقم اعشار
            }
        }


        // محاسبه قطر واقعی مانیتور (اینچ)
        //public static double GetMonitorDiagonalInches(Screen screen)
        //{
        //    int widthPx = screen.Bounds.Width;
        //    int heightPx = screen.Bounds.Height;

        //    var (dpiX, dpiY) = GetMonitorDpi(screen);

        //    double widthInches = widthPx / dpiX;
        //    double heightInches = heightPx / dpiY;

        //    return Math.Sqrt(widthInches * widthInches + heightInches * heightInches);
        //}
    }
}
