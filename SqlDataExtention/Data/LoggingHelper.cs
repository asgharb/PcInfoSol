using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDataExtention.Data
{
    public class LoggingHelper
    {
        private readonly DataHelper _dataHelper = new DataHelper();

        public void Log(string entityName, string action, object keyValue, string message)
        {
            try
            {
                string query = @"INSERT INTO [Log] ([EntityName], [ActionType], [PrimaryKeyValue], [Message]) 
                                 VALUES (@EntityName, @ActionType, @KeyValue, @Message)";

                _dataHelper.ExecuteNonQuery(query,
                    new SqlParameter("@EntityName", entityName ?? "-"),
                    new SqlParameter("@ActionType", action ?? "-"),
                    new SqlParameter("@KeyValue", keyValue?.ToString() ?? "-"),
                    new SqlParameter("@Message", message ?? "-"));
            }
            catch
            {
                // در صورت خطا، فقط ادامه بده (نباید برنامه بخوابه)
            }
        }
    }
}
