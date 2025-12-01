using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using static SqlDataExtention.Utils.SystemInfoComparer;

namespace SqlDataExtention.Data
{
    public class DataInsertUpdateHelper
    {
        private readonly DataHelper _dataHelper;
        private readonly string _connectionString;

        public DataInsertUpdateHelper()
        {
            _dataHelper = new DataHelper();
            _connectionString = _dataHelper.ConnectionString;
        }

        #region Insert 
        public bool InsertSimple<T>(T obj, out object primaryKeyValue)
        {
            primaryKeyValue = null;
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p))
                            .ToList();

            var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
            var parameters = props.Select(p => $"@{p.Name}").ToList();

            string query = $@"
                             INSERT INTO [{tableName}] ({string.Join(", ", columns)})
                             OUTPUT INSERTED.[{keyColumn}]
                             VALUES ({string.Join(", ", parameters)})";

            var sqlParams = props
                .Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value))
                .ToArray();

            try
            {
                primaryKeyValue = _dataHelper.ExecuteScalar(query, sqlParams);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<(bool Success, object PrimaryKey)> InsertSimple<T>(List<T> items)
        {
            var results = new List<(bool, object)>();
            if (items == null || items.Count == 0) return results;

            foreach (var item in items)
            {
                bool success = InsertSimple(item, out var pk);
                results.Add((success, pk));
            }
            return results;
        }

        public bool InsertWithChildren<T>(T obj, out object primaryKeyValue)
        {
            primaryKeyValue = null;
            using (var conn = _dataHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Insert parent
                    primaryKeyValue = InsertSingle(obj, conn, tran);

                    var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T));
                    keyProp.SetValue(obj, Convert.ChangeType(primaryKeyValue, keyProp.PropertyType));

                    // Insert children (one level)
                    InsertChildren(obj, conn, tran);

                    tran.Commit();
                    return true;
                }
                catch
                {
                    tran.Rollback();
                    return false;
                }
            }
        }

        private object InsertSingle<T>(T obj, SqlConnection conn, SqlTransaction tran)
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p))
                            .ToList();

            var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
            var parameters = props.Select(p => $"@{p.Name}").ToList();

            string query = $@"
INSERT INTO [{tableName}] ({string.Join(", ", columns)})
OUTPUT INSERTED.[{keyColumn}]
VALUES ({string.Join(", ", parameters)})";

            var sqlParams = props
                .Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value))
                .ToArray();

            using (var cmd = new SqlCommand(query, conn, tran))
            {
                cmd.Parameters.AddRange(sqlParams);
                return cmd.ExecuteScalar();
            }
        }

        private void InsertChildren(object obj, SqlConnection conn, SqlTransaction tran)
        {
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Where(p => EntityMetadataHelper.IsIgnored(p));

            var parentKey = EntityMetadataHelper.GetPrimaryKeyProperty(obj.GetType()).GetValue(obj);

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var child = prop.GetValue(obj);
                    if (child == null) continue;

                    var fkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, obj.GetType());
                    if (fkProp != null) fkProp.SetValue(child, parentKey);

                    InsertSingleDynamic(child, conn, tran);
                }
                else if (propType.IsGenericType)
                {
                    var list = prop.GetValue(obj) as IEnumerable;
                    if (list == null) continue;

                    foreach (var item in list)
                    {
                        var fkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, obj.GetType());
                        if (fkProp != null) fkProp.SetValue(item, parentKey);

                        InsertSingleDynamic(item, conn, tran);
                    }
                }
            }
        }

        private void InsertSingleDynamic(object obj, SqlConnection conn, SqlTransaction tran)
        {
            var type = obj.GetType();
            var method = typeof(DataInsertUpdateHelper)
                .GetMethod(nameof(InsertSingle), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(type);

            var key = method.Invoke(this, new object[] { obj, conn, tran });
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            keyProp.SetValue(obj, key);
        }

        private bool InsertWithChildrenSingle<T>(T obj, SqlConnection conn, SqlTransaction tran, out object primaryKeyValue)
        {
            primaryKeyValue = null;
            if (obj == null) return false;

            try
            {
                var type = typeof(T);
                string tableName = EntityMetadataHelper.GetTableName(type);
                var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
                string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p))
                                .ToList();

                var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
                var parameters = props.Select(p => $"@{p.Name}").ToList();

                string query = $@"
INSERT INTO [{tableName}] ({string.Join(", ", columns)})
OUTPUT INSERTED.[{keyColumn}]
VALUES ({string.Join(", ", parameters)})";

                var sqlParams = props
                    .Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value))
                    .ToArray();

                using (var cmd = new SqlCommand(query, conn, tran))
                {
                    cmd.Parameters.AddRange(sqlParams);
                    primaryKeyValue = cmd.ExecuteScalar();
                }

                // ست کردن کلید اصلی در نمونه
                keyProp.SetValue(obj, Convert.ChangeType(primaryKeyValue, keyProp.PropertyType));
                return true;
            }
            catch
            {
                primaryKeyValue = null;
                return false;
            }
        }

        public bool InsertMappingResults(List<SwithInfo> items)
        {
            if (items == null || items.Count == 0)
                return false;

            // 1) مرتب‌سازی
            var sortedItems = items
                .OrderBy(x => x.SwitchIp)
                .ThenBy(x => x.SwitchPort)
                .ToList();

            // 2) Insert بدون خروجی
            int successCount = 0;

            foreach (var item in sortedItems)
            {
                if (InsertSimpleWithoutOuput(item))
                    successCount++;
            }

            if (successCount == 0)
                return false;

            // 3) بعد از Insert اجرا شود
            UpdateSystemInfoRefAfterInsert();

            return true;
        }

        public bool InsertSimpleWithoutOuput<T>(T obj)
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !EntityMetadataHelper.IsIgnored(p) &&
                                        !EntityMetadataHelper.IsDbGenerated(p))
                            .ToList();

            var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
            var parameters = props.Select(p => $"@{p.Name}").ToList();

            string query = $@"
        INSERT INTO [{tableName}] ({string.Join(", ", columns)})
        VALUES ({string.Join(", ", parameters)})";

            var sqlParams = props
                .Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value))
                .ToArray();

            try
            {
                _dataHelper.ExecuteNonQuery(query, sqlParams);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool InsertSimpleWithoutOuput<T>(List<T> items)
        {
            if (items == null || items.Count == 0)
                return true; // هیچی نبود یعنی مشکلی نیست

            foreach (var item in items)
            {
                if (!InsertSimpleWithoutOuput(item))
                    return false; // اگر یک مورد fail شد، بقیه هم مهم نیست
            }

            return true;
        }


        //        public void UpdateSystemInfoRefAfterInsert()
        //        {
        //            var select = new DataSelectHelper();
        //            var dataHelper = new DataHelper();

        //            // 1) خواندن NetworkAdapterInfo (Active)
        //            var adapters = select.SelectAllWitoutConditonal<NetworkAdapterInfo>()
        //                .Where(x => !string.IsNullOrEmpty(x.MACAddress))
        //                .ToList();

        //            if (adapters == null || adapters.Count == 0) return;

        //            // 2) خواندن SwithInfo هایی که SystemInfoRef = 0 یا NULL هستند
        //            string getSwitchInfoQuery = @"
        //SELECT * FROM [SwithInfo]
        //WHERE (SystemInfoRef IS NULL OR SystemInfoRef = 0)
        //  AND (ExpireDate IS NULL)";

        //            var dtSwitch = dataHelper.ExecuteQuery(getSwitchInfoQuery);
        //            var openSwitchInfos = dataHelper.ConvertToList<SwithInfo>(dtSwitch);

        //            if (openSwitchInfos == null || openSwitchInfos.Count == 0) return;

        //            using (var conn = dataHelper.GetConnectionClosed())
        //            {
        //                conn.Open();

        //                // 3) آپدیت SwithInfo بر اساس MAC
        //                foreach (var sw in openSwitchInfos)
        //                {
        //                    if (string.IsNullOrEmpty(sw.PcMac))
        //                        continue;

        //                    string swMac = NormalizeMac(sw.PcMac);

        //                    var match = adapters.FirstOrDefault(a =>
        //                        NormalizeMac(a.MACAddress) == swMac);

        //                    if (match == null)
        //                        continue;

        //                    string updateQuery = @"
        //UPDATE [SwithInfo]
        //SET SystemInfoRef = @SysRef,
        //    PcIp = @PcIp
        //WHERE SwithInfoID = @Id";

        //                    using (var cmd = new SqlCommand(updateQuery, conn))
        //                    {
        //                        cmd.Parameters.AddWithValue("@SysRef", match.SystemInfoRef);
        //                        cmd.Parameters.AddWithValue("@PcIp",
        //                            string.IsNullOrEmpty(match.IpAddress)
        //                                ? (object)DBNull.Value
        //                                : match.IpAddress);
        //                        cmd.Parameters.AddWithValue("@Id", sw.SwithInfoID);

        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }

        //                // 4) آپدیت ستون UserFullName در [SwithInfo] بر اساس [PcCodeInfo]
        //                string updateUserQuery = @"
        //UPDATE sw
        //SET sw.UserFullName = pc.UserFullName
        //FROM [SwithInfo] sw
        //INNER JOIN [PcCodeInfo] pc
        //    ON sw.SystemInfoRef = pc.SystemInfoRef
        //WHERE pc.ExpireDate IS NULL
        //  AND sw.SystemInfoRef IS NOT NULL";  // فقط ردیف‌های معتبر


        //                using (var cmd = new SqlCommand(updateUserQuery, conn))
        //                {
        //                    cmd.ExecuteNonQuery();
        //                }

        //                conn.Close();
        //            }
        //        }

        public void UpdateSystemInfoRefAfterInsert()
        {
            var select = new DataSelectHelper();
            var dataHelper = new DataHelper();


            string updateSwithInfo = @"UPDATE Sw
                 SET Sw.SystemInfoRef = n.SystemInfoRef,
                     Sw.PcIp = n.IpAddress,
                     Sw.UserFullName = pci.UserFullName
                 FROM dbo.SwithInfo Sw
                 INNER JOIN dbo.NetworkAdapterInfo n 
                     ON Sw.PcMac IS NOT NULL
                     AND n.MACAddress IS NOT NULL
                     AND REPLACE(TRIM(Sw.PcMac), ':', '') = REPLACE(TRIM(n.MACAddress), ':', '')
                 INNER JOIN dbo.PcCodeInfo pci 
                     ON pci.SystemInfoRef = n.SystemInfoRef
                 WHERE n.ExpireDate IS NULL
                 AND pci.ExpireDate IS NULL;
                 ";

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var cmd = new SqlCommand(updateSwithInfo, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }






            string deleteQuery_Duplicate = @"
  WITH Duplicates AS (
    SELECT 
        SwithInfoID, 
        SwitchIp, 
        SwitchPort,
        PcMac,
        PhoneMac,
        -- به هر سطر یک شماره ردیف می‌دهیم
        ROW_NUMBER() OVER (
            -- 1. گروه‌بندی بر اساس پورت و سوئیچ (چون مشکل روی یک پورت خاص است)
            PARTITION BY SwitchIp, SwitchPort 
            
            -- 2. اولویت‌بندی برای نگهداری (هرکدام بالاتر باشد می‌ماند)
            ORDER BY 
                -- الف: سطری که هم PC و هم Phone دارد بالاترین اولویت را دارد
                CASE WHEN PcMac IS NOT NULL AND PhoneMac IS NOT NULL THEN 3 
                     WHEN PcMac IS NOT NULL THEN 2 -- ب: فقط PC دارد
                     WHEN PhoneMac IS NOT NULL THEN 1 -- ج: فقط تلفن دارد
                     ELSE 0 END DESC,
                
                -- د: اگر از نظر پر بودن مساوی بودند، اونی که جدیدتر اینسرت شده بماند
                InsertDate DESC 
        ) AS RowNum
    FROM SwithInfo
)
-- تمام سطرهایی که رتبه ۱ نشدند (یعنی تکراری یا ناقص هستند) را حذف کن
DELETE FROM Duplicates 
WHERE RowNum > 1;

";

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var cmd = new SqlCommand(deleteQuery_Duplicate, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }





            string deleteQuery = @"DELETE FROM PcInfo.dbo.SystemEnvironmentInfo
WHERE SystemEnvironmentInfoID NOT IN
(
    SELECT MAX(SystemEnvironmentInfoID) AS KeepId
    FROM PcInfo.dbo.SystemEnvironmentInfo
    GROUP BY
        ComputerName,
        UserName,
        Domain,
        OperatingSystem,
        OsVersion,
        IsRealVNCInstalled,
        IsSemanticInstalled,
        SystemInfoRef
);

  DELETE FROM [PcInfo].[dbo].[SwithInfo]
WHERE [SwithInfoID] NOT IN
(
    SELECT MAX([SwithInfoID]) AS KeepId
    FROM [PcInfo].[dbo].[SwithInfo]
    GROUP BY
       [SwitchIp]
      ,[SwitchPort]
      ,[PcMac]
      ,[PcVlan]
      ,[PcIp]
      ,[PhoneMac]
      ,[PhoneVlan]
      ,[PhoneIp]
      ,[UserFullName]
      ,[SystemInfoRef]
      ,[VTMac]
      ,[VTIP]
      ,[VTVlan]
);
";

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var cmd = new SqlCommand(deleteQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }


        }



        #endregion


        #region ApplyDifferences 
        private void ExpireByForeignKey_Internal(Type entityType, object foreignKeyValue, SqlConnection conn, SqlTransaction tran)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (conn == null) throw new ArgumentNullException(nameof(conn));

            string tableName = EntityMetadataHelper.GetTableName(entityType);
            string fkColumn = "SystemInfoRef";

            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{fkColumn}] = @Fk AND [ExpireDate] IS NULL";

            using (var cmd = new SqlCommand(query, conn, tran))
            {
                cmd.Parameters.Add(new SqlParameter("@Now", DateTime.Now));
                cmd.Parameters.Add(new SqlParameter("@Fk", foreignKeyValue ?? DBNull.Value));
                cmd.ExecuteNonQuery();
            }
        }
        public void ApplyDifferences(SystemInfo currentSystemInfo, List<Difference> diffs)
        {
            if (currentSystemInfo == null) throw new ArgumentNullException(nameof(currentSystemInfo));
            if (diffs == null || diffs.Count == 0) return;

            // گروه‌بندی براساس EntityType
            var groupedByEntity = diffs
                .Where(d => d.EntityType != null)
                .GroupBy(d => d.EntityType)
                .ToList();

            // parent key (SystemInfo primary key) از currentSystemInfo
            var parentKeyProp = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(SystemInfo));
            var parentKeyValue = parentKeyProp.GetValue(currentSystemInfo);

            // باز کردن connection و transaction یکتا برای همه عملیات
            SqlConnection conn = null;
            SqlTransaction tran = null;
            try
            {
                conn = _dataHelper.GetConnectionClosed();
                conn.Open();
                tran = conn.BeginTransaction();

                foreach (var group in groupedByEntity)
                {
                    Type entityType = group.Key;
                    // FK: اگر diffs شامل ForeignKeyValue باشد از آن استفاده کن، وگرنه از parentKeyValue
                    object fkFromDiff = group.FirstOrDefault(d => d.ForeignKeyValue != null)?.ForeignKeyValue;
                    object fkValue = fkFromDiff ?? parentKeyValue;

                    // 1) bulk Expire براساس entityType و fkValue داخل همان تراکنش
                    ExpireByForeignKey_Internal(entityType, fkValue, conn, tran);

                    // 2) پیدا کردن پروپرتی مربوط در SystemInfo براساس نوع
                    // ابتدا دنبال List<entityType>
                    PropertyInfo listProp = typeof(SystemInfo)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p =>
                            typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                            && p.PropertyType != typeof(string)
                            && p.PropertyType.IsGenericType
                            && p.PropertyType.GetGenericArguments()[0] == entityType
                        );

                    if (listProp != null)
                    {
                        var listObj = listProp.GetValue(currentSystemInfo) as IEnumerable;
                        if (listObj != null)
                        {
                            foreach (var item in listObj.Cast<object>())
                            {
                                // ست کردن FK (SystemInfoRef) در هر آیتم اگر پروپرتی وجود دارد
                                var fkProp = item.GetType().GetProperty("SystemInfoRef", BindingFlags.Public | BindingFlags.Instance);
                                if (fkProp != null && parentKeyValue != null)
                                {
                                    try { fkProp.SetValue(item, Convert.ChangeType(parentKeyValue, fkProp.PropertyType)); }
                                    catch { /* ignore conversion errors */ }
                                }
                                // Insert هر آیتم با همان connection/tran
                                InsertSingleDynamic(item, conn, tran);
                            }
                        }

                        // ادامه به گروه بعدی
                        continue;
                    }

                    // اگر لیست پیدا نشد، دنبال single property باش
                    PropertyInfo singleProp = typeof(SystemInfo)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => p.PropertyType == entityType);

                    if (singleProp != null)
                    {
                        var obj = singleProp.GetValue(currentSystemInfo);
                        if (obj != null)
                        {
                            var fkProp = obj.GetType().GetProperty("SystemInfoRef", BindingFlags.Public | BindingFlags.Instance);
                            if (fkProp != null && parentKeyValue != null)
                            {
                                try { fkProp.SetValue(obj, Convert.ChangeType(parentKeyValue, fkProp.PropertyType)); }
                                catch { }
                            }

                            InsertSingleDynamic(obj, conn, tran);
                        }
                    }
                }
                tran.Commit();
            }
            catch
            {
                if (tran != null) tran.Rollback();
                throw;
            }
            finally
            {
                if (tran != null) tran.Dispose();
                if (conn != null) conn.Dispose();
            }
        }
        public bool ExpireAndInsertPcCodeInfo(int systemInfoRef, PcCodeInfo newItem)
        {
            if (newItem == null) throw new ArgumentNullException(nameof(newItem));

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Expire تمام رکوردهای موجود با SystemInfoRef و ExpireDate NULL
                        string expireQuery = @"
UPDATE [PcCodeInfo] 
SET [ExpireDate] = @Now 
WHERE [SystemInfoRef] = @Fk AND [ExpireDate] IS NULL";

                        using (var cmd = new SqlCommand(expireQuery, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Fk", systemInfoRef);
                            cmd.ExecuteNonQuery();
                        }

                        // 2) Set FK در نمونه ورودی
                        newItem.SystemInfoRef = systemInfoRef;

                        // 3) Insert رکورد جدید
                        object pk;
                        bool success = InsertWithChildrenSingle(newItem, conn, tran, out pk);

                        if (success)
                        {
                            tran.Commit();
                            return success;
                        }
                        else
                        {
                            tran.Rollback();
                            return false;
                        }

                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
        #endregion


    }
}




//#region Expire Helpers
//private void ExpireRow(object obj, SqlConnection conn, SqlTransaction tran)
//{
//    if (obj == null) return;

//    Type type = obj.GetType();
//    string tableName = EntityMetadataHelper.GetTableName(type);
//    var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
//    object keyValue = keyProp.GetValue(obj);
//    string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

//    string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{keyColumn}] = @Key";
//    var parameters = new[]
//    {
//                new SqlParameter("@Now", DateTime.Now),
//                new SqlParameter("@Key", keyValue ?? DBNull.Value)
//            };

//    using (var cmd = new SqlCommand(query, conn, tran))
//    {
//        cmd.Parameters.AddRange(parameters);
//        cmd.ExecuteNonQuery();
//    }
//}

//public void ExpireByForeignKey(Type entityType, object foreignKeyValue)
//{
//    if (entityType == null)
//        throw new ArgumentNullException(nameof(entityType));
//    if (foreignKeyValue == null)
//        throw new ArgumentNullException(nameof(foreignKeyValue));

//    string tableName = EntityMetadataHelper.GetTableName(entityType);
//    string fkColumn = "SystemInfoRef";

//    using (var conn = _dataHelper.GetConnectionClosed())
//    {
//        conn.Open();
//        using (var tran = conn.BeginTransaction())
//        {
//            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{fkColumn}] = @Fk";
//            using (var cmd = new SqlCommand(query, conn, tran))
//            {
//                cmd.Parameters.AddWithValue("@Now", DateTime.Now);
//                cmd.Parameters.AddWithValue("@Fk", foreignKeyValue);
//                cmd.ExecuteNonQuery();
//            }

//            tran.Commit();
//        }
//    }
//}
//#endregion