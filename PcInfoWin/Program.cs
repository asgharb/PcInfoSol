using PcInfoWin.Properties;
using SqlDataExtention.Data;
using SqlDataExtention.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using SqlDataExtention.Entity.Main;
using SqlDataExtention.Entity;
using System.Linq;
using PcInfoWin.Extention;

namespace PcInfoWin
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            showSplashScreen();

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
                        PcCodeForm.IsNewMode=false;
                        MessageBox.Show("با موفقیت درج شد", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    }
}


/*
using PcInfoWin.Properties;
using SqlDataExtention.Data;
using SqlDataExtention.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using SqlDataExtention.Entity.Main;
using SqlDataExtention.Entity;
using System.Linq;

namespace PcInfoWin
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Settings.Default.SystemInfoID = -1;
            Settings.Default.PersonnelCode = 0;
            Settings.Default.PcCode = "0"; 
            Settings.Default.Save();

            //SchemaGeneratorAdvanced schemaGeneratorAdvanced = new SchemaGeneratorAdvanced();
            //schemaGeneratorAdvanced.CreateSysmtemAllTabels ();

            var selector = new DataSelectHelper();
            DataInsertUpdateHelper dataUpdateHelper = new DataInsertUpdateHelper();

            SystemInfo curreentInfo = SystemInfoHelper.GetCurentSystemInfo();
            int inProgramSystemInfoID = checkSettings();
            SystemInfo infoFromDB = new SystemInfo();

            if (Settings.Default.SystemInfoID > 0)
            {
                infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(Settings.Default.SystemInfoID);

                curreentInfo.SystemInfoID = Settings.Default.SystemInfoID;
                updateCurreentInfoFromSetting(curreentInfo);


                if (infoFromDB != null)
                {
                    var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);

                    if (differences.Count == 0)
                    {
                        //Console.WriteLine("No difference was found.");
                    }
                    else
                    {
                        //Console.WriteLine("Differences:");
                        //foreach (var diff in differences)
                        //{
                        //    Console.WriteLine();
                        //    Console.WriteLine(diff);
                        //}
                        dataUpdateHelper.ApplyDifferences(curreentInfo, differences);
                    }
                }
            }
            else
            {

                List<NetworkAdapterInfo> adapterInfo = selector.SelectByColumn<NetworkAdapterInfo>(nameof(NetworkAdapterInfo.MACAddress), curreentInfo.NetworkAdapterInfo[0].MACAddress);
                if (adapterInfo != null && adapterInfo.Count > 0)
                {
                    infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(adapterInfo[0].SystemInfoRef);


                    curreentInfo.SystemInfoID = infoFromDB.SystemInfoID;
                    curreentInfo.pcCodeInfo[0].PcCodeInfoID = infoFromDB.pcCodeInfo[0].PcCodeInfoID;
                    curreentInfo.pcCodeInfo[0].SystemInfoRef = infoFromDB.pcCodeInfo[0].SystemInfoRef;
                    curreentInfo.pcCodeInfo[0].PcCode = Settings.Default.PcCode = infoFromDB.pcCodeInfo[0].PcCode;
                    curreentInfo.pcCodeInfo[0].PersonnelCode = Settings.Default.PersonnelCode = infoFromDB.pcCodeInfo[0].PersonnelCode;
                    curreentInfo.pcCodeInfo[0].UserFullName = Settings.Default.UserFullName = infoFromDB.pcCodeInfo[0].UserFullName;
                    curreentInfo.pcCodeInfo[0].Unit = Settings.Default.Unit = infoFromDB.pcCodeInfo[0].Unit;
                    curreentInfo.pcCodeInfo[0].Desc1 = Settings.Default.Desc1 = infoFromDB.pcCodeInfo[0].Desc1;
                    curreentInfo.pcCodeInfo[0].Desc2 = Settings.Default.Desc2 = infoFromDB.pcCodeInfo[0].Desc2;
                    curreentInfo.pcCodeInfo[0].Desc3 = Settings.Default.Desc2 = infoFromDB.pcCodeInfo[0].Desc3;
                    curreentInfo.pcCodeInfo[0].InsertDate = infoFromDB.pcCodeInfo[0].InsertDate;
                    Settings.Default.Save();


                    var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);


                    if (differences != null && differences.Count > 0)
                    {
                        //bool allMatch = (differences?.Any() ?? false) &&
                        //                differences.All(x => x.EntityType != null &&
                        //                                     x.EntityType.FullName == "SqlDataExtention.Entity.PcCodeInfo");

                        dataUpdateHelper.ApplyDifferences(curreentInfo, differences);
                        updateSetting(curreentInfo);

                    }
                }
                else
                {
                    PcCodeForm.IsEditMode = true;
                    using (var form = new PcCodeForm())
                    {
                        form.ShowDialog();
                    }
                    if (string.IsNullOrWhiteSpace(Settings.Default.PcCode) && !PcCodeForm.resultImportData)
                    {
                        //MessageBox.Show("خطا در ثبت اطلاعات: ", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(0);
                    }

                    updateCurreentInfoFromSetting(curreentInfo);
                    bool success = dataUpdateHelper.InsertWithChildren<SystemInfo>(curreentInfo, out var mainKey);
                    Settings.Default.SystemInfoID = int.Parse(mainKey.ToString());
                    Settings.Default.Save();
                    if (success)
                    {
                        //Console.WriteLine($"Insert Complated: {mainKey}");
                    }
                    else
                    {
                        //Console.WriteLine("The insert operation encountered an Error and was Rolled Back.");
                    }
                }
            }


            TrayApplication.IpAddress = curreentInfo.NetworkAdapterInfo[0].IpAddress;
            TrayApplication.MacAddress = curreentInfo.NetworkAdapterInfo[0].MACAddress;
            using (var trayApp = new TrayApplication())
            {
                Application.Run();
            }
        }


        public static int checkSettings()
        {
            try
            {
                return Settings.Default.SystemInfoID;
            }
            catch (Exception ex)
            {
                string filename = ((ConfigurationErrorsException)ex).Filename;
                if (File.Exists(filename))
                    File.Delete(filename);
                Application.Restart();
                return -1;
            }
        }

        public static void updateCurreentInfoFromSetting(SystemInfo curreentInfo)
        {
            curreentInfo.pcCodeInfo[0].PcCode = Settings.Default.PcCode;
            curreentInfo.pcCodeInfo[0].UserFullName = Settings.Default.UserFullName;
            curreentInfo.pcCodeInfo[0].PersonnelCode = Settings.Default.PersonnelCode;
            curreentInfo.pcCodeInfo[0].Unit = Settings.Default.Unit;
            curreentInfo.pcCodeInfo[0].Desc1 = Settings.Default.Desc1;
            curreentInfo.pcCodeInfo[0].Desc2 = Settings.Default.Desc2;
            curreentInfo.pcCodeInfo[0].Desc3 = Settings.Default.Desc3;
        }
        public static void updateSetting(SystemInfo curreentInfo)
        {
            Settings.Default.SystemInfoID = curreentInfo.SystemInfoID;
            Settings.Default.PcCode = curreentInfo.pcCodeInfo[0].PcCode;
            Settings.Default.UserFullName = curreentInfo.pcCodeInfo[0].UserFullName;
            Settings.Default.PersonnelCode = curreentInfo.pcCodeInfo[0].PersonnelCode;
            Settings.Default.Unit = curreentInfo.pcCodeInfo[0].Unit;
            Settings.Default.Desc1 = curreentInfo.pcCodeInfo[0].Desc1;
            Settings.Default.Desc2 = curreentInfo.pcCodeInfo[0].Desc2;
            Settings.Default.Desc3 = curreentInfo.pcCodeInfo[0].Desc3;
            Settings.Default.Save();
        }


    }

}

 */
