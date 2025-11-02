using System;
using System.Data.SqlClient;


namespace SqlDataExtention.Data
{
    public static class LoggingHelper
    {
        public static void LogError(Exception ex, string additionalInfo = null, int? SysId = null)
        {
            DataHelper _dataHelper = new DataHelper();

            try
            {
                string query = @"
                INSERT INTO [dbo].[AppErrorLog]
                (SysId, ErrorMessage, StackTrace, InnerMessage, AdditionalInfo)
                VALUES (@SysId, @ErrorMessage, @StackTrace, @InnerMessage, @AdditionalInfo)";

                _dataHelper.ExecuteNonQuery(query,
                     new SqlParameter("@SysId", SysId.HasValue ? SysId.Value : (object)DBNull.Value),
                     //new SqlParameter("@MachineName", Environment.MachineName ?? (object)DBNull.Value),
                     //new SqlParameter("@FormName", formName ?? (object)DBNull.Value),
                     //new SqlParameter("@MethodName", methodName ?? (object)DBNull.Value),
                     new SqlParameter("@ErrorMessage", ex.Message ?? (object)DBNull.Value),
                     new SqlParameter("@StackTrace", ex.StackTrace ?? (object)DBNull.Value),
                     new SqlParameter("@InnerMessage", ex.InnerException?.Message ?? (object)DBNull.Value),
                     new SqlParameter("@AdditionalInfo", additionalInfo ?? (object)DBNull.Value)
                     );
            }
            catch
            {

            }
        }
    }
}
