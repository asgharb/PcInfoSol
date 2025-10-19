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


            //var insertor = new DataInsertUpdateHelper();
            //bool success = insertor.InsertWithChildren<SystemInfo>(curreentInfo, out var mainKey);


            var selector = new DataSelectHelper();
            DataInsertUpdateHelper dataUpdateHelper = new DataInsertUpdateHelper(); ;

            SystemInfo curreentInfo = SystemInfoHelper.GetCurentSystemInfo();

            int inProgramSystemInfoID = -1;
            try
            {
                inProgramSystemInfoID = Settings.Default.SystemInfoID;
            }
            catch (Exception ex)
            {
                string filename = ((ConfigurationErrorsException)ex).Filename;
                if (File.Exists(filename))
                    File.Delete(filename);
                Application.Restart();
            }
            if (Settings.Default.SystemInfoID > 0)
            {
                curreentInfo.SystemInfoID = Settings.Default.SystemInfoID;
                curreentInfo.pcCodeInfo[0].PcCode = Settings.Default.PcCode;

                SystemInfo infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(curreentInfo.SystemInfoID);
                if (infoFromDB != null)
                {
                    Console.WriteLine("No record found in database for the given SystemInfoID.");
                    var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);

                    if (differences.Count == 0)
                    {
                        Console.WriteLine("No difference was found.");
                    }
                    else
                    {
                        Console.WriteLine("Differences:");
                        foreach (var diff in differences)
                        {
                            Console.WriteLine();
                            Console.WriteLine(diff);
                        }

                       

                        dataUpdateHelper.SyncSingleTableByDiff(curreentInfo, infoFromDB, differences);
                    }
                }

            }
            else
            {
                //var selector = new DataSelectHelper();
                //List<NetworkAdapterInfo> adapterInfo = selector.SelectByColumnValue<NetworkAdapterInfo>(nameof(NetworkAdapterInfo.MACAddress), curreentInfo.NetworkAdapterInfo[0].MACAddress);
                //if (adapterInfo != null && adapterInfo.Count > 0)
                //{
                //    var macAddress = curreentInfo.NetworkAdapterInfo[0].MACAddress;
                //    int sysId = curreentInfo.NetworkAdapterInfo[0].SystemInfoRef;
                //    SystemInfo infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(sysId);
                //    var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);
                //    if (differences.Count == 0)
                //        Console.WriteLine("No difference was found.");
                //    else
                //    {
                //        Console.WriteLine("Differences:");
                //        foreach (var diff in differences)
                //        {
                //            Console.WriteLine();
                //            Console.WriteLine(diff);
                //        }

                //        DataUpdateHelper dataUpdateHelper = new DataUpdateHelper();

                //        dataUpdateHelper.ApplySystemInfoDifferences(differences, 33);
                //    }
                //}
                //else
                //{
                //    using (var form = new PcCodeForm())
                //    {
                //        form.IsEditMode = true;
                //        form.ShowDialog();
                //    }

                //    var insertor = new DataInsertHelper();
                //    bool success = insertor.InsertWithRelationsTransaction(curreentInfo, out var mainKey);

                //    if (success)
                //    {
                //        Console.WriteLine($"Insert Complated: {mainKey}");
                //    }
                //    else
                //    {
                //        Console.WriteLine("The insert operation encountered an Error and was Rolled Back.");
                //    }
                //}
            }

            using (var trayApp = new TrayApplication())
            {
                // این خط باعث می‌شود برنامه تا زمانی که برنامه بسته شود فعال بماند
                Application.Run();
            }

        }
    }
}
