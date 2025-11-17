using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcInfoWin
{
    public class ScriptRunResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdErr { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class RunPsScriptsHelper
    {
        // timeout در میلی‌ثانیه (مثلاً 2 دقیقه)
        private const int ScriptTimeoutMs = 120_000;

        // مسیر لاگ فایل پشتیبان در صورت خطا در نوشتن دیتابیس
        //private static string FallbackLogPath =>
        //    Path.Combine(Application.StartupPath, "ErrorLogScripts.txt");

        // مسیر لاگ فایل پشتیبان در کنار فایل exe برنامه
        private static string FallbackLogPath = Path.Combine(Application.StartupPath, "ErrorLogScripts.txt");


        public static async Task<List<ScriptRunResult>> RunAllPs1InFolderAsync(string folderPath)
        {
            var results = new List<ScriptRunResult>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    var r = new ScriptRunResult
                    {
                        FilePath = folderPath,
                        Success = false,
                        ErrorMessage = "Folder does not exist"
                    };
                    results.Add(r);
                    return results;
                }

                var files = Directory.GetFiles(folderPath, "*.ps1", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    return results; // خالی
                }

                foreach (var file in files)
                {
                    // اجرا را برای هر فایل در try-catch جدا قرار می‌دهیم
                    var result = await Task.Run(() => RunSinglePs1(file));
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                // خطاهای کلی‌تر را لاگ و fallback کنیم
                SafeLog(ex, $"RunAllPs1InFolderAsync folder:{folderPath}");
                results.Add(new ScriptRunResult
                {
                    FilePath = folderPath,
                    Success = false,
                    ErrorMessage = "Unhandled error: " + ex.Message
                });
            }

            return results;
        }

        private static ScriptRunResult RunSinglePs1(string filePath)
        {
            var res = new ScriptRunResult { FilePath = filePath };

            try
            {
                if (!File.Exists(filePath))
                {
                    res.Success = false;
                    res.ErrorMessage = "File not found";
                    LogExecutionResult(res); // لاگ حتی خطای پیدا نکردن فایل
                    return res;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(filePath)
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        res.Success = false;
                        res.ErrorMessage = "Process could not be started";
                        LogExecutionResult(res);
                        return res;
                    }

                    string stdOut = process.StandardOutput.ReadToEnd();
                    string stdErr = process.StandardError.ReadToEnd();

                    bool exited = process.WaitForExit(ScriptTimeoutMs);
                    if (!exited)
                    {
                        try { process.Kill(); } catch { }

                        res.Success = false;
                        res.StdOut = stdOut;
                        res.StdErr = stdErr;
                        res.ErrorMessage = $"Timeout after {ScriptTimeoutMs / 1000} seconds";
                        LogExecutionResult(res);
                        return res;
                    }

                    res.ExitCode = process.ExitCode;
                    res.StdOut = stdOut;
                    res.StdErr = stdErr;
                    res.Success = (process.ExitCode == 0 && string.IsNullOrWhiteSpace(stdErr));

                    if (!res.Success)
                    {
                        res.ErrorMessage = "Script returned non-zero or produced stderr.";
                    }

                    // ثبت لاگ **حتی اگر موفق باشد**
                    LogExecutionResult(res);
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.ErrorMessage = ex.Message;
                SafeLog(ex, $"Running script: {filePath}");
                LogExecutionResult(res); // ثبت لاگ اجرای با خطا
            }

            return res;
        }

        // لاگ امن با fallback به فایل متنی (اگر LoggingHelper خطا بدهد)
        private static void SafeLog(Exception ex, string additionalInfo = null)
        {
            try
            {
                // اگر LoggingHelper در پروژه‌ات هست از آن استفاده کن
                // SqlDataExtention.Data.LoggingHelper.LogError(Exception ex, string additionalInfo = null, int? SysId = null)
                try
                {
                    // اگر SystemInfoID دارید میتوانید مقدارش را قرار دهید
                    SqlDataExtention.Data.LoggingHelper.LogError(ex, additionalInfo, null);
                    return;
                }
                catch (Exception logEx)
                {
                    // اگر لاگ دیتابیس هم خطا داد، به فایل پشتیبان مینویسیم
                    File.AppendAllText(
                        FallbackLogPath,
                        DateTime.UtcNow.ToString("o") + " | LoggingHelper failed: " + logEx + Environment.NewLine);
                }

                // نوشتن اطلاعات اصلی خطا در فایل پشتیبان
                File.AppendAllText(
                    FallbackLogPath,
                    DateTime.UtcNow.ToString("o") + " | " + additionalInfo + " | " + ex + Environment.NewLine);
            }
            catch
            {
                // هر خطای نهایی را ساکت کنیم تا اجرا قطع نشود
            }
        }

        private static void LogExecutionResult(ScriptRunResult res)
        {
            try
            {
                string logLine = $"{DateTime.UtcNow:o} | File: {res.FilePath} | Success: {res.Success} | ExitCode: {res.ExitCode} | Error: {res.ErrorMessage} | StdErr Length: {res.StdErr?.Length ?? 0}";
                File.AppendAllText(FallbackLogPath, logLine + Environment.NewLine);
            }
            catch
            {
                // اگر باز هم نوشتن در فایل مشکل داشت، نادیده گرفته شود
            }
        }

    }
}
