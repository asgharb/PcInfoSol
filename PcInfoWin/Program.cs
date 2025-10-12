using PcInfoWin.Data;
using PcInfoWin.Entity.Main;
using PcInfoWin.Entity.Models;
using PcInfoWin.Properties;
using PcInfoWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcInfoWin
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //SystemInfo curreentInfo = new SystemInfo();

            //if (Settings.Default.SystemInfoID > 0)
            //{
            //    curreentInfo.SystemInfoID = Settings.Default.SystemInfoID;

            //    var selector = new DataSelectHelper();

            //    SystemInfo infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(curreentInfo.SystemInfoID);
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
            //    var selector = new DataSelectHelper();
            //    List<NetworkAdapterInfo> adapterInfo = selector.SelectByColumnValue<NetworkAdapterInfo>(nameof(NetworkAdapterInfo.MACAddress), curreentInfo.NetworkAdapterInfo[0].MACAddress);
            //    if (adapterInfo != null && adapterInfo.Count > 0)
            //    {
            //        var macAddress = curreentInfo.NetworkAdapterInfo[0].MACAddress;
            //    }
            //    else
            //    {
            //        var insertor = new DataInsertHelper();
            //        bool success = insertor.InsertWithRelationsTransaction(curreentInfo, out var mainKey);

            //        if (success)
            //        {
            //            Console.WriteLine($"Insert Complated: {mainKey}");
            //        }
            //        else
            //        {
            //            Console.WriteLine("The insert operation encountered an Error and was Rolled Back.");
            //        }
            //    }
            //}

            using (var trayApp = new TrayApplication())
            {
                // این خط باعث می‌شود برنامه تا زمانی که برنامه بسته شود فعال بماند
                Application.Run();
            }

        }
    }
}
