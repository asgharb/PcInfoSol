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
using System.Reflection;
using System.Windows.Forms;

namespace PcInfoWin
{
    internal static class Program
    {
        public static string defaultUpdatePath= @"\\172.20.7.53\soft\PcInfo\Release";

        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);



            CheckForUpdate();

            try
            {
                Receiver receiver = new Receiver(9000);
                receiver.StartListening();

                showSplashScreen();

                //////Application.Run();

                if (!CheckSettings())
                {
                    MessageBox.Show("اجرای برنامه با خطا مواجه شده به واحد انفورماتیک اطلاع بدید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }

                var dataHelper = new DataHelper();
                bool isConnected = dataHelper.TestConnection();

                if (isConnected)
                {
                    var selector = new DataSelectHelper();
                    DataInsertUpdateHelper dataUpdateHelper = new DataInsertUpdateHelper();

                    SystemInfo curreentInfo = SystemInfoHelper.GetCurentSystemInfo();
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
                            Environment.Exit(0);
                        }

                        ExtentionMethode.updateCurreentInfoFromUserInput(curreentInfo);

                        bool success = dataUpdateHelper.InsertWithChildren<SystemInfo>(curreentInfo, out var mainKey);
                        if (success)
                        {
                            ExtentionMethode.updateSettingsDefaultFromCurreentInfo(curreentInfo);
                            ExtentionMethode.updateBalonInfoFromSettings();
                            PcCodeForm.IsNewMode = false;
                            //MessageBox.Show("با موفقیت درج شد", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("در درج اطلاعات با خطا مواجه شدیم.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Environment.Exit(0);
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


        public static void CheckForUpdate()
        {
            try
            {
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string updatePath = defaultUpdatePath;
                if (!string.IsNullOrEmpty(Settings.Default.PathUpdate) && Settings.Default.PathUpdate.Length > 4)
                {
                    updatePath = Settings.Default.PathUpdate;
                }

                MessageBox.Show("11111111111111111111", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string versionFile = Path.Combine(updatePath, "version.txt");

                if (!File.Exists(versionFile)) return;

                string newVersion = File.ReadAllText(versionFile).Trim();

                MessageBox.Show("22222222222222222222222222222222", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (newVersion != currentVersion)
                {
                    MessageBox.Show("currentVersion:" + currentVersion, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show("newVersion:"+ newVersion, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // مسیر فایل Updater.exe
                    string AutoUpdaterePath = Path.Combine(Application.StartupPath, "PcInfoAutoUpdater.exe");
                    Process.Start(AutoUpdaterePath, $"\"{updatePath}\"");

                    Environment.Exit(0);
                }
            }
            catch { }
        }

    }
}

