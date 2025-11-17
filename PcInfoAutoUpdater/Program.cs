using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Threading;

namespace PcInfoAutoUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //// بررسی وجود درایو D
                //if (DriveInfo.GetDrives().Any(d => d.Name.StartsWith("D:", StringComparison.OrdinalIgnoreCase)))
                //{
                //    string dPath = @"D:\DournaCo\PcInfoClient";
                //    if (Directory.Exists(dPath))
                //    {
                //        installPath = dPath;
                //    }
                //    else if (installPath == null && DriveInfo.GetDrives().Any(d => d.Name.StartsWith("E:", StringComparison.OrdinalIgnoreCase)))
                //    {
                //        string ePath = @"E:\DournaCo\PcInfoClient";
                //        if (Directory.Exists(ePath))
                //        {
                //            installPath = ePath;
                //        }
                //        else if (installPath == null && DriveInfo.GetDrives().Any(d => d.Name.StartsWith("V:", StringComparison.OrdinalIgnoreCase)))
                //        {
                //            string vPath = @"V:\DournaCo\PcInfoClient";
                //            if (Directory.Exists(ePath))
                //            {
                //                installPath = vPath;
                //            }
                //            else if (installPath == null && DriveInfo.GetDrives().Any(d => d.Name.StartsWith("G:", StringComparison.OrdinalIgnoreCase)))
                //            {
                //                string gPath = @"G:\DournaCo\PcInfoClient";
                //                if (Directory.Exists(gPath))
                //                {
                //                    installPath = gPath;
                //                }
                //            }
                //            else if (installPath == null && DriveInfo.GetDrives().Any(d => d.Name.StartsWith("F:", StringComparison.OrdinalIgnoreCase)))
                //            {
                //                string fPath = @"F:\DournaCo\PcInfoClient";
                //                if (Directory.Exists(fPath))
                //                {
                //                    installPath = fPath;
                //                }
                //                else if (installPath == null && DriveInfo.GetDrives().Any(d => d.Name.StartsWith("H:", StringComparison.OrdinalIgnoreCase)))
                //                {
                //                    string hPath = @"H:\DournaCo\PcInfoClient";
                //                    if (Directory.Exists(hPath))
                //                    {
                //                        installPath = hPath;
                //                    }
                //                    else if (installPath == null)
                //                    {
                //                        installPath = @"C:\DournaCo\PcInfoClient";
                //                    }
                //                }
                //            }


                // مسیر نصب فعلی
                string installPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // مسیر نسخه جدید در پوشه شبکه
                string updatePath = (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])) ? args[0] : @"\\172.20.7.53\soft\PcInfo\Release";

                // نام برنامه اصلی
                string mainExe = "PcInfoWin.exe";

                Console.WriteLine("Waiting for MainApp to close...");
                Thread.Sleep(2000);

                // بستن هر نمونه باز از برنامه
                foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(mainExe)))
                {
                    p.Kill();
                    p.WaitForExit();
                }

                Console.WriteLine("Copying new files...");
                // کپی فایل‌های جدید از شبکه
                CopyFiles(updatePath, installPath);

                Console.WriteLine("Update complete. Starting application...");
                // اجرای مجدد برنامه
                //Process.Start(Path.Combine(installPath, mainExe));
                Process.Start(mainExe);

                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Update failed: " + ex.Message);
                Thread.Sleep(4000);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        static void CopyFiles(string sourceDir, string destDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                // نام فایل فعلی بدون مسیر
                string fileName = Path.GetFileName(file);

                // اگر فایل جزو استثناهاست، ردش کن
                if (string.Equals(fileName, "PcInfoAutoUpdater.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = file.Substring(sourceDir.Length).TrimStart('\\');
                string targetFile = Path.Combine(destDir, relativePath);
                string targetDir = Path.GetDirectoryName(targetFile);

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(file, targetFile, true);
            }
        }

    }
}
