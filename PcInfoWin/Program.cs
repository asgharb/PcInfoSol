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
                    if (string.IsNullOrWhiteSpace(Settings.Default.PcCode) && PcCodeForm.resultImportData)
                    {
                        //MessageBox.Show("خطا در ثبت اطلاعات: ", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
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
