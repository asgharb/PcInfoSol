using PcInfoWin.Data;
using PcInfoWin.Entity.Main;
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

            SystemInfo curreentInfo= new SystemInfo();

            var selector = new DataSelectHelper();

            //SystemInfo infoFromDB = selector.SelectSystemInfoWithRelations(33);
            SystemInfo infoFromDB = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(33);
            var differences = SystemInfoComparer.CompareSystemInfo(curreentInfo, infoFromDB);


            if (differences.Count == 0)
                Console.WriteLine("No difference was found.");
            else
            {
                Console.WriteLine("Differences:");
                foreach (var diff in differences)
                    Console.WriteLine(diff);
            }


            if (Settings.Default.SystemInfoID >0)
            {

            }



            //var insertor = new DataInsertHelper();

            //// فراخوانی تابع اصلی
            //bool success = insertor.InsertWithRelationsTransaction(curreentInfo, out var mainKey);

            //if (success)
            //{
            //    Console.WriteLine($"Insert Complated: {mainKey}");
            //}
            //else
            //{
            //    Console.WriteLine("The insert operation encountered an Error and was Rolled Back.");
            //}


 

            //Console.WriteLine(systemInfo.ToString());

            Application.Run();

            Console.ReadKey();
        }
    }
}
