using System;
using System.Diagnostics;
using System.IO;
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
                // مسیر نصب فعلی
                string installPath = @"D:\Program Files\Pcinfo";
                // مسیر نسخه جدید در پوشه شبکه
                string updatePath = (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))? args[0]: @"\\172.20.7.53\soft\PcInfo\Release";


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
                Process.Start(Path.Combine(installPath, mainExe));

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

//internal static class Program
//{
//    private static string networkPath = @"\\172.20.7.53\Soft\PcInfo";
//    private static string debugFolderName = "Debug";
//    private static string localBasePath = @"D:\PcInfo";
//    private static string logFilePath = Path.Combine(localBasePath, debugFolderName, "AutoUpdater.log");

//    private static int RetrySeconds = 5;
//    private static int MaxRetries = 5;

//    [STAThread]
//    private static void Main()
//    {
//        try
//        {
//            RunUpdater();
//        }
//        catch (Exception ex)
//        {
//            WriteLog("خطای غیرمنتظره: " + ex.Message);
//        }
//        Environment.Exit(0);
//    }

//    private static void RunUpdater()
//    {
//        try
//        {
//            string networkDebugPath = Path.Combine(networkPath, debugFolderName);
//            if (!Directory.Exists(networkDebugPath))
//            {
//                WriteLog($"پوشه شبکه یافت نشد: {networkDebugPath}");
//                return;
//            }

//            if (!Directory.Exists(localBasePath))
//                Directory.CreateDirectory(localBasePath);

//            string localDebugPath = Path.Combine(localBasePath, debugFolderName);

//            // پاک کردن پوشه قدیمی
//            if (Directory.Exists(localDebugPath))
//            {
//                try
//                {
//                    Directory.Delete(localDebugPath, true);
//                }
//                catch (Exception ex)
//                {
//                    WriteLog("خطا در حذف پوشه قدیمی: " + ex.Message);
//                    return;
//                }
//            }

//            // کپی پوشه جدید با تلاش مجدد
//            bool copySuccess = false;
//            for (int i = 1; i <= MaxRetries; i++)
//            {
//                try
//                {
//                    CopyDirectory(networkDebugPath, localDebugPath);
//                    copySuccess = true;
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    WriteLog($"تلاش {i}/{MaxRetries} برای کپی با خطا مواجه شد: {ex.Message}");
//                    Thread.Sleep(RetrySeconds * 1000);
//                }
//            }

//            if (!copySuccess)
//            {
//                WriteLog("کپی پوشه Debug پس از چندین تلاش انجام نشد.");
//                return;
//            }

//            // اجرای فایل exe
//            string exePath = Path.Combine(localDebugPath, "PcInfoWin.exe");
//            if (File.Exists(exePath))
//            {
//                try
//                {
//                    Process.Start(new ProcessStartInfo
//                    {
//                        FileName = exePath,
//                        WorkingDirectory = localDebugPath,
//                        UseShellExecute = true
//                    });
//                }
//                catch (Exception ex)
//                {
//                    WriteLog("خطا در اجرای برنامه: " + ex.Message);
//                }
//            }
//            else
//            {
//                WriteLog("فایل PcInfoWin.exe در پوشه جدید یافت نشد.");
//            }
//        }
//        catch (Exception ex)
//        {
//            WriteLog("خطای کلی در فرآیند: " + ex.Message);
//        }
//    }

//    private static void CopyDirectory(string sourceDir, string destDir)
//    {
//        Directory.CreateDirectory(destDir);
//        foreach (string file in Directory.GetFiles(sourceDir))
//        {
//            string dest = Path.Combine(destDir, Path.GetFileName(file));
//            File.Copy(file, dest, true);
//        }
//        foreach (string dir in Directory.GetDirectories(sourceDir))
//        {
//            string dest = Path.Combine(destDir, Path.GetFileName(dir));
//            CopyDirectory(dir, dest);
//        }
//    }

//    private static void WriteLog(string message)
//    {
//        try
//        {
//            string folder = Path.GetDirectoryName(logFilePath);
//            if (!Directory.Exists(folder))
//                Directory.CreateDirectory(folder);

//            File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
//        }
//        catch
//        {
//            // اگر نوشتن لاگ هم شکست خورد، هیچ کاری نکن
//        }
//    }
//}
