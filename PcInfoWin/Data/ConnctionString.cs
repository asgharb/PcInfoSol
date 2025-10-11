using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Data
{
    public static class ConnctionString
    {
        //private readonly string _connectionString = "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;";
        //private static readonly string _connectionString = @"Data Source=(local)\SQLEXPRESS;Initial Catalog=MyDatabase;Integrated Security=True;";
        private static readonly string _connectionString = @"Server=172.20.7.4;Database=PcInfo;User Id=sa;Password=Aa#12369;";

        public static string GetConnctionString()
        {
            return _connectionString;
        }
    }
}
