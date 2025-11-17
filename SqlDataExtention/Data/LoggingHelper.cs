using System;
using System.Data.SqlClient;


namespace SqlDataExtention.Data
{
    public static class LoggingHelper
    {
        public static void LogError(Exception ex, string additionalInfo = null, int? SysId = null)
        {
            try
            {
                DataHelper _dataHelper = new DataHelper();

                string query = @"
        INSERT INTO [dbo].[AppErrorLog]
        (SysId, ErrorMessage, ExceptionType, StackTrace, InnerMessage, AdditionalInfo)
        VALUES (@SysId, @ErrorMessage, @ExceptionType, @StackTrace, @InnerMessage, @AdditionalInfo)";

                _dataHelper.ExecuteNonQuery(query,
                     new SqlParameter("@SysId", SysId ?? (object)DBNull.Value),
                     new SqlParameter("@ErrorMessage", ex.Message ?? (object)DBNull.Value),
                     new SqlParameter("@ExceptionType", ex.GetType().FullName),
                     new SqlParameter("@StackTrace", ex.StackTrace ?? (object)DBNull.Value),
                     new SqlParameter("@InnerMessage", ex.InnerException?.Message ?? (object)DBNull.Value),
                     new SqlParameter("@AdditionalInfo", additionalInfo ?? (object)DBNull.Value)
                );
            }
            catch (Exception logEx)
            {
                // اگر هم دیتابیس خطا داد، می‌توان در فایل لاگ ذخیره کرد
                System.IO.File.AppendAllText("ErrorLog.txt", DateTime.Now + " : " + logEx.ToString() + Environment.NewLine);
            }
        }

    }
}


//new SqlParameter("@MachineName", Environment.MachineName ?? (object)DBNull.Value),
//new SqlParameter("@FormName", formName ?? (object)DBNull.Value),
//new SqlParameter("@MethodName", methodName ?? (object)DBNull.Value)
