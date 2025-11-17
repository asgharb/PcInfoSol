using Newtonsoft.Json;
using PcInfoWin.Extention;
using PcInfoWin.Message;
using PcInfoWin.Properties;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using SqlDataExtention.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PcInfoWin
{
    internal static class Program
    {
        public static string defaultUpdatePath = @"\\172.20.7.53\soft\PcInfo\Release";
        public static Receiver receiver;

        [STAThread]
        static void Main()
        {

            //RunScripts();

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "PcInfoWin", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("برنامه هم‌اکنون در حال اجرا است.", "توجه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // جلوگیری از اجرای دوباره
                }


                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);


                WirteVersion();
                Thread.Sleep(2000);
                if (Environment.MachineName != "Bizaval-pc" && Environment.MachineName != "BIZAVAL-PC")
                    CheckForUpdate();
                Thread.Sleep(2000);
                showSplashScreen();

                receiver = new Receiver(9000);
                try
                {
                    Thread.Sleep(2000);
                    if (!CheckSettings())
                    {
                        MessageBox.Show("برنامه نمیتواند به دیتابیس وصل بشود ", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        receiver.StopListening();
                        Environment.Exit(0);
                    }

                    receiver.StartListening();

                    var dataHelper = new DataHelper();
                    bool isConnected = dataHelper.TestConnection();

                    if (isConnected)
                    {
                        var selector = new DataSelectHelper();
                        DataInsertUpdateHelper dataUpdateHelper = new DataInsertUpdateHelper();

                        SystemInfo curreentInfo = SystemInfoHelper.GetCurentSystemInfo();

                        if (curreentInfo.NetworkAdapterInfo == null || curreentInfo.NetworkAdapterInfo.Count == 0)
                        {
                            MessageBox.Show("برنامه نمیتواند اطلاعات کارت شبکه را دریافت کند.لطفا با واحد انفورماتیک تماس بگیرید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            receiver.StopListening();
                            Environment.Exit(0);
                        }
                        SystemInfo infoFromDB = new SystemInfo();

                        List<NetworkAdapterInfo> adapterInfo = selector.SelectByColumn<NetworkAdapterInfo>(nameof(NetworkAdapterInfo.MACAddress), curreentInfo.NetworkAdapterInfo[0].MACAddress);
                        if (adapterInfo != null && adapterInfo.Count > 0)
                        {
                            infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(adapterInfo[0].SystemInfoRef);

                            ExtentionMethode.updateCurreentInfoFromDBInfo(curreentInfo, infoFromDB);

                            var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);

                            if (differences != null && differences.Count > 0)
                            {
                                dataUpdateHelper.ApplyDifferences(curreentInfo, differences);
                            }
                            ExtentionMethode.updateSettingsDefaultFromCurreentInfo(curreentInfo);
                            ExtentionMethode.updateBalonInfoFromSettings();
                        }
                        else
                        {
                            PcCodeForm.IsNewMode = true;
                            using (var form = new PcCodeForm())
                            {
                                form.ShowDialog();
                            }
                            if (string.IsNullOrWhiteSpace(PcCodeForm._pcCodeInfo.PcCode) && !PcCodeForm.resultImportData)
                            {
                                receiver.StopListening();
                                Environment.Exit(0);
                            }

                            ExtentionMethode.updateCurreentInfoFromUserInput(curreentInfo);

                            bool success = dataUpdateHelper.InsertWithChildren<SystemInfo>(curreentInfo, out var mainKey);
                            if (success)
                            {
                                ExtentionMethode.updateSettingsDefaultFromCurreentInfo(curreentInfo);
                                ExtentionMethode.updateBalonInfoFromSettings();
                                PcCodeForm.IsNewMode = false;
                                MessageBox.Show("با موفقیت درج شد", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("در درج اطلاعات با خطا مواجه شدیم.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                receiver.StopListening();
                                Environment.Exit(0);
                            }
                        }
                        PcCodeForm._pcCodeInfo = curreentInfo.pcCodeInfo[0];
                    }
                    else
                    {
                        ExtentionMethode.updateBalonInfoFromSettings();
                    }
                    using (var trayApp = new TrayApplication())
                    {
                        Application.Run();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("اجرای برنامه با خطا مواجه شده به واحد انفورماتیک اطلاع بدید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LoggingHelper.LogError(ex, "---", SysId: Settings.Default.SystemInfoID > 0 ? Settings.Default.SystemInfoID : 0);
                    receiver.StopListening();
                    Environment.Exit(0);
                }
            }

        }

        public static bool CheckSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;

                // لیست کلیدها و مقادیر پیش‌فرض اولیه برای زمانی که کلید وجود ندارد
                var defaultValues = new Dictionary<string, string>
        {
            { "PcCode", "0000" },
            { "IpAddress", "0.0.0.0" },
            { "MacAddress", "00:00:00:00:00:00" },
            { "Desc1", "" }
        };

                bool updated = false;

                foreach (var item in defaultValues)
                {
                    var key = item.Key;
                    var defaultValue = item.Value;

                    // بررسی وجود کلید در تنظیمات
                    var property = settings.Properties[key];

                    if (property == null)
                    {
                        // فقط اگر وجود نداشت، بسازش
                        settings.Properties.Add(
                            new System.Configuration.SettingsProperty(key)
                            {
                                DefaultValue = defaultValue,
                                IsReadOnly = false,
                                PropertyType = typeof(string),
                                Provider = settings.Providers["LocalFileSettingsProvider"],
                                SerializeAs = System.Configuration.SettingsSerializeAs.String
                            });

                        // مقدار اولیه (فقط برای کلید جدید)
                        settings[key] = defaultValue;
                        updated = true;
                    }
                }

                // فقط در صورت اضافه شدن کلید جدید ذخیره شود
                if (updated)
                    settings.Save();

                return true; // یعنی حالا همه کلیدها وجود دارند
            }
            catch (ConfigurationErrorsException confEx)
            {
                // در صورت خرابی فایل config
                if (File.Exists(confEx.Filename))
                    File.Delete(confEx.Filename);

                LoggingHelper.LogError(confEx, "---", SysId: Settings.Default.SystemInfoID > 0 ? Settings.Default.SystemInfoID : 0);

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void showSplashScreen()
        {
            SplashScreen slScreen = new SplashScreen();
            slScreen.StartPosition = FormStartPosition.CenterParent;
            slScreen.ShowDialog();
        }

        public static void WirteVersion()
        {
            try
            {
                string versionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");

                if (File.Exists(versionFile))
                    File.Delete(versionFile);

                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                File.WriteAllText(versionFile, version);

                //Console.WriteLine("Version file updated: " + version);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "---", SysId: Settings.Default.SystemInfoID > 0 ? Settings.Default.SystemInfoID : 0);
                showSplashScreen();
            }
        }

        public static void CheckForUpdate()
        {
            int x = 254;
            try
            {
                // نسخه فعلی برنامه
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                // مسیر آپدیت
                string updatePath = defaultUpdatePath;
                if (!string.IsNullOrEmpty(Settings.Default.PathUpdate) && Settings.Default.PathUpdate.Length > 4)
                {
                    updatePath = Settings.Default.PathUpdate;
                }
                x = 264;
                string versionFile = Path.Combine(updatePath, "version.txt");
                x = 266;
                // بررسی وجود فایل version.txt
                if (!File.Exists(versionFile))
                {
                    x = 270;
                    LoggingHelper.LogError(
                        new FileNotFoundException("Version file not found" + " X:" + x.ToString(), versionFile),
                        $"Checked update path: {updatePath}",
                        Settings.Default.SystemInfoID
                    );
                    return;
                }
                x = 278;
                string newVersion;
                try
                {
                    newVersion = File.ReadAllText(versionFile).Trim();
                    x = 283;
                }
                catch (Exception ex)
                {
                    LoggingHelper.LogError(
                        ex,
                        $"Failed to read version file: {versionFile}" + " X:" + x.ToString(),
                        Settings.Default.SystemInfoID
                    );
                    return;
                }
                x = 294;
                // بررسی نسخه جدید
                if (newVersion != currentVersion)
                {
                    string autoUpdaterPath = Path.Combine(Application.StartupPath, "PcInfoAutoUpdater.exe");
                    x = 299;
                    if (!File.Exists(autoUpdaterPath))
                    {
                        LoggingHelper.LogError(
                            new FileNotFoundException("AutoUpdater executable not found" + " X:" + x.ToString(), autoUpdaterPath),
                            $"Current version: {currentVersion}, New version: {newVersion}",
                            Settings.Default.SystemInfoID
                        );
                        return;
                    }

                    try
                    {
                        x = 312;
                        Process.Start(autoUpdaterPath, $"\"{updatePath}\"");
                        receiver.StopListening();
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        x = 319;
                        LoggingHelper.LogError(
                            ex,
                            $"Failed to start updater: {autoUpdaterPath}" + " X:" + x.ToString(),
                            Settings.Default.SystemInfoID
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                x = 330;
                LoggingHelper.LogError(
                    ex,
                    "---Unhandled exception in CheckForUpdate---" + " X:" + x.ToString(),
                    Settings.Default.SystemInfoID
                );
            }
        }

        private static async void RunScripts()
        {
            string folder = @"\\172.20.7.53\Soft\PcInfoScripts"; // یا مسیر محلی
            var results = await RunPsScriptsHelper.RunAllPs1InFolderAsync(folder);

            // نمایش خلاصه به کاربر (مثال)
            var sb = new StringBuilder();
            foreach (var r in results)
            {
                sb.AppendLine($"{Path.GetFileName(r.FilePath)} => Success: {r.Success}, Error: {r.ErrorMessage}");
            }
            //MessageBox.Show(sb.ToString(), "Run scripts result");
        }
    }
}
