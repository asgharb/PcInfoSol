using PcInfoWin.Data;
using PcInfoWin.Entity.Main;
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

            var insertor = new DataInsertHelper();

            // فراخوانی تابع اصلی
            bool success = insertor.InsertWithRelationsTransaction(curreentInfo, out var mainKey);

            if (success)
            {
                Console.WriteLine($"Insert Complated: {mainKey}");
            }
            else
            {
                Console.WriteLine("The insert operation encountered an Error and was Rolled Back.");
            }


            //var selector = new DataSelectHelper();
            //SystemInfo systemInfo = selector.SelectWithRelationsByPrimaryKey<SystemInfo>(mainKey);
            //systemInfo.ToString();

            //Console.WriteLine(systemInfo.ToString());

            Application.Run();

            Console.ReadKey();
        }
    }
}
