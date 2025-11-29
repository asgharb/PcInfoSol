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

        public bool InsertList<T>(List<T> items)
        {
            if (items == null || items.Count == 0)
                return false;

            var results = InsertSimple(items);
            return results.All(r => r.Success);
        }


        public bool InsertMappingResults(List<SwithInfo> items)
        {
            var successes = new List<SwithInfo>();

            // 1) INSERT
            foreach (var item in items)
            {
                if (InsertSimple(item, out var pk))
                {
                    // ⭐⭐ مهم‌ترین خط ⭐⭐
                    if (pk != null && pk != DBNull.Value)
                        item.SwithInfoID = Convert.ToInt32(pk);

                    successes.Add(item);
                }
            }

            if (successes.Count == 0)
                return false;


            // 2) Update SystemInfoRef
            UpdateSystemInfoRefAfterInsert();


            // 3) Reload
            var refreshed = ReloadInserted(successes);


            // 4) Expire
            var grouped = refreshed
                .Where(x => x.SystemInfoRef != 0)
                .GroupBy(x => x.SystemInfoRef);

            foreach (var group in grouped)
            {
                int sysRef = group.Key;
                ExpireOldSwithInfo(sysRef);
            }

            return true;
        }

        public void UpdateSystemInfoRefAfterInsert()
        {
            var select = new DataSelectHelper();
            var dataHelper = new DataHelper();

            // 1) خواندن NetworkAdapterInfo (Active)
            var adapters = select.SelectAllWitoutConditonal<NetworkAdapterInfo>()
                .Where(x => !string.IsNullOrEmpty(x.MACAddress))
                .ToList();

            if (adapters == null || adapters.Count == 0) return;

            // 2) خواندن SwithInfo هایی که SystemInfoRef = 0 یا NULL هستند
            string getSwitchInfoQuery = @"
        SELECT * FROM [SwithInfo]
        WHERE (SystemInfoRef IS NULL OR SystemInfoRef = 0)
          AND (ExpireDate IS NULL)";

            var dtSwitch = dataHelper.ExecuteQuery(getSwitchInfoQuery);
            var openSwitchInfos = dataHelper.ConvertToList<SwithInfo>(dtSwitch);

            if (openSwitchInfos == null || openSwitchInfos.Count == 0) return;

            using (var conn = dataHelper.GetConnectionClosed())
            {
                conn.Open();

                foreach (var sw in openSwitchInfos)
                {
                    if (string.IsNullOrEmpty(sw.PcMac))
                        continue;

                    string swMac = NormalizeMac(sw.PcMac);

                    var match = adapters.FirstOrDefault(a =>
                        NormalizeMac(a.MACAddress) == swMac);

                    if (match == null)
                        continue;

                    // آپدیت هم SystemInfoRef و هم PcIp
                    string updateQuery = @"
                UPDATE [SwithInfo]
                SET SystemInfoRef = @SysRef,
                    PcIp = @PcIp
                WHERE SwithInfoID = @Id";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@SysRef", match.SystemInfoRef);
                        cmd.Parameters.AddWithValue("@PcIp",
                            string.IsNullOrEmpty(match.IpAddress)
                                ? (object)DBNull.Value
                                : match.IpAddress);
                        cmd.Parameters.AddWithValue("@Id", sw.SwithInfoID);

                        cmd.ExecuteNonQuery();
                    }
                }
                conn.Close();
            }
        }

        public void ExpireOldSwithInfo(int systemInfoRef)
        {
            string query = @"
UPDATE [SwithInfo]
SET [ExpireDate] = @Now
WHERE [SystemInfoRef] = @Fk
  AND [SwithInfoID] <> (
        SELECT MAX(SwithInfoID)
        FROM [SwithInfo]
        WHERE [SystemInfoRef] = @Fk
    );
";

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Fk", systemInfoRef);
                    cmd.ExecuteNonQuery();
                }
            }

            string query2 = @"DELETE s  FROM [PcInfo].[dbo].[SwithInfo] s
                              WHERE s.ExpireDate IS NOT NULL";

            using (var conn2 = _dataHelper.GetConnectionClosed())
            {
                conn2.Open();
                using (var cmd2 = new SqlCommand(query2, conn2))
                {
                    cmd2.ExecuteNonQuery();
                }
            }
        }


        private List<SwithInfo> ReloadInserted(List<SwithInfo> oldOnes)
        {
            var dataHelper = new DataHelper();

            var ids = oldOnes
                .Select(x => x.SwithInfoID)
                .Where(id => id > 0)
                .ToList();

            if (ids.Count == 0)
                return new List<SwithInfo>();

            string idList = string.Join(",", ids);

            string q = $"SELECT * FROM SwithInfo WHERE SwithInfoID IN ({idList})";

            var dt = dataHelper.ExecuteQuery(q);
            return dataHelper.ConvertToList<SwithInfo>(dt);
        }

        public string NormalizeMac(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac))
                return null;

            return mac
                .Replace(":", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace(" ", "")
                .Trim()
                .ToUpper();
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