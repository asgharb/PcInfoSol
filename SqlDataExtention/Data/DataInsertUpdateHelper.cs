//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Reflection;
//using SqlDataExtention.Attributes;
//using SqlDataExtention.Entity.Main;
//using SqlDataExtention.Utils;
//using static SqlDataExtention.Utils.SystemInfoComparer;

//namespace SqlDataExtention.Data
//{
//    public class DataInsertUpdateHelper
//    {
//        private readonly DataHelper _dataHelper;

//        public DataInsertUpdateHelper()
//        {
//            _dataHelper = new DataHelper();
//        }

//        #region Insert Simple
//        public bool InsertSimple<T>(T obj, out object primaryKeyValue)
//        {
//            primaryKeyValue = null;
//            Type type = typeof(T);
//            string tableName = EntityMetadataHelper.GetTableName(type);
//            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
//            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

//            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
//                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p))
//                            .ToList();

//            var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
//            var parameters = props.Select(p => $"@{p.Name}").ToList();

//            string query = $@"
//INSERT INTO [{tableName}] ({string.Join(", ", columns)})
//OUTPUT INSERTED.[{keyColumn}]
//VALUES ({string.Join(", ", parameters)})";

//            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToArray();

//            try
//            {
//                primaryKeyValue = _dataHelper.ExecuteScalar(query, sqlParams);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public List<(bool Success, object PrimaryKey)> InsertSimple<T>(List<T> items)
//        {
//            var results = new List<(bool, object)>();
//            if (items == null || items.Count == 0) return results;

//            foreach (var item in items)
//            {
//                bool success = InsertSimple(item, out var pk);
//                results.Add((success, pk));
//            }
//            return results;
//        }
//        #endregion

//        #region Insert With Children (One-Level)
//        public bool InsertWithChildren<T>(T obj, out object primaryKeyValue)
//        {
//            primaryKeyValue = null;
//            using (var conn = _dataHelper.GetConnection())
//            using (var tran = conn.BeginTransaction())
//            {
//                try
//                {
//                    // Insert parent
//                    primaryKeyValue = InsertSingle(obj, conn, tran);

//                    var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T));
//                    keyProp.SetValue(obj, Convert.ChangeType(primaryKeyValue, keyProp.PropertyType));

//                    // Insert children (one level)
//                    InsertChildren(obj, conn, tran);

//                    tran.Commit();
//                    return true;
//                }
//                catch
//                {
//                    tran.Rollback();
//                    return false;
//                }
//            }
//        }

//        private object InsertSingle<T>(T obj, SqlConnection conn, SqlTransaction tran)
//        {
//            Type type = typeof(T);
//            string tableName = EntityMetadataHelper.GetTableName(type);
//            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
//            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

//            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
//                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p))
//                            .ToList();

//            var columns = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}]").ToList();
//            var parameters = props.Select(p => $"@{p.Name}").ToList();

//            string query = $@"
//INSERT INTO [{tableName}] ({string.Join(", ", columns)})
//OUTPUT INSERTED.[{keyColumn}]
//VALUES ({string.Join(", ", parameters)})";

//            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToArray();

//            using (var cmd = new SqlCommand(query, conn, tran))
//            {
//                cmd.Parameters.AddRange(sqlParams);
//                return cmd.ExecuteScalar();
//            }
//        }

//        private void InsertChildren(object obj, SqlConnection conn, SqlTransaction tran)
//        {
//            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
//                           .Where(p => EntityMetadataHelper.IsIgnored(p));

//            var parentKey = EntityMetadataHelper.GetPrimaryKeyProperty(obj.GetType()).GetValue(obj);

//            foreach (var prop in props)
//            {
//                Type propType = prop.PropertyType;

//                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
//                {
//                    var child = prop.GetValue(obj);
//                    if (child == null) continue;

//                    var fkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, obj.GetType());
//                    if (fkProp != null) fkProp.SetValue(child, parentKey);

//                    InsertSingleDynamic(child, conn, tran);
//                }
//                else if (propType.IsGenericType)
//                {
//                    var list = prop.GetValue(obj) as IEnumerable;
//                    if (list == null) continue;

//                    foreach (var item in list)
//                    {
//                        var fkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, obj.GetType());
//                        if (fkProp != null) fkProp.SetValue(item, parentKey);

//                        InsertSingleDynamic(item, conn, tran);
//                    }
//                }
//            }
//        }

//        private void InsertSingleDynamic(object obj, SqlConnection conn, SqlTransaction tran)
//        {
//            var type = obj.GetType();
//            var method = typeof(DataInsertUpdateHelper)
//                .GetMethod(nameof(InsertSingle), BindingFlags.NonPublic | BindingFlags.Instance)
//                .MakeGenericMethod(type);

//            var key = method.Invoke(this, new object[] { obj, conn, tran });
//            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
//            keyProp.SetValue(obj, key);
//        }

//        // فقط wrapper برای سازگاری با نام InsertRow
//        private void InsertRow(object obj, SqlConnection conn, SqlTransaction tran)
//        {
//            InsertSingleDynamic(obj, conn, tran);
//        }
//        #endregion

//        #region Update or Insert Children Only
//        public void UpdateOrInsertChildrenOnly(object dbParent, object newParent)
//        {
//            if (dbParent == null || newParent == null)
//                throw new ArgumentNullException();

//            using (var conn = _dataHelper.GetConnection())
//            using (var tran = conn.BeginTransaction())
//            {
//                try
//                {
//                    var childProps = dbParent.GetType()
//                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
//                        .Where(p => Attribute.IsDefined(p, typeof(IgnoreAttribute)));

//                    foreach (var prop in childProps)
//                    {
//                        Type propType = prop.PropertyType;

//                        if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
//                        {
//                            var dbChild = prop.GetValue(dbParent);
//                            var newChild = prop.GetValue(newParent);
//                            if (newChild == null) continue;

//                            if (dbChild != null)
//                            {
//                                var diffs = SystemInfoComparer.CompareSystemInfo(newChild, dbChild);
//                                if (diffs.Count > 0)
//                                {
//                                    ExpireRow(dbChild, conn, tran);
//                                    InsertSingleDynamic(newChild, conn, tran);
//                                }
//                            }
//                            else
//                            {
//                                InsertSingleDynamic(newChild, conn, tran);
//                            }
//                        }
//                        else if (propType.IsGenericType)
//                        {
//                            var dbList = (prop.GetValue(dbParent) as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
//                            var newList = (prop.GetValue(newParent) as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

//                            int count = Math.Min(dbList.Count, newList.Count);
//                            for (int i = 0; i < count; i++)
//                            {
//                                var diffs = SystemInfoComparer.CompareSystemInfo(newList[i], dbList[i]);
//                                if (diffs.Count > 0)
//                                {
//                                    ExpireRow(dbList[i], conn, tran);
//                                    InsertSingleDynamic(newList[i], conn, tran);
//                                }
//                            }

//                            for (int i = count; i < newList.Count; i++)
//                            {
//                                InsertSingleDynamic(newList[i], conn, tran);
//                            }
//                        }
//                    }

//                    tran.Commit();
//                }
//                catch
//                {
//                    tran.Rollback();
//                    throw;
//                }
//            }
//        }
//        #endregion

//        #region Expire Row

//        private void ExpireRow(object obj, SqlConnection conn, SqlTransaction tran)
//        {
//            if (obj == null) return;

//            Type type = obj.GetType();
//            string tableName = EntityMetadataHelper.GetTableName(type);
//            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
//            object keyValue = keyProp.GetValue(obj);
//            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

//            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{keyColumn}] = @Key";
//            var parameters = new[]
//            {
//        new SqlParameter("@Now", DateTime.Now),
//        new SqlParameter("@Key", keyValue ?? DBNull.Value)
//    };

//            using (var cmd = new SqlCommand(query, conn, tran))
//            {
//                cmd.Parameters.AddRange(parameters);
//                cmd.ExecuteNonQuery();
//            }
//        }

//        public void ExpireByForeignKey(Type entityType, object foreignKeyValue)
//        {
//            if (entityType == null)
//                throw new ArgumentNullException(nameof(entityType));
//            if (foreignKeyValue == null)
//                throw new ArgumentNullException(nameof(foreignKeyValue));

//            // گرفتن نام جدول از نوع
//            string tableName = EntityMetadataHelper.GetTableName(entityType);
//            if (string.IsNullOrEmpty(tableName))
//                throw new Exception($"Table name for type {entityType.Name} not found.");

//            string fkColumn = "SystemInfoRef"; // فرض ثابت

//            using (var conn = _dataHelper.GetConnectionClosed())
//            {
//                conn.Open();
//                using (var tran = conn.BeginTransaction())
//                {
//                    try
//                    {
//                        string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{fkColumn}] = @Fk";
//                        using (var cmd = new SqlCommand(query, conn, tran))
//                        {
//                            cmd.Parameters.AddWithValue("@Now", DateTime.Now);
//                            cmd.Parameters.AddWithValue("@Fk", foreignKeyValue);
//                            cmd.ExecuteNonQuery();
//                        }

//                        tran.Commit();
//                    }
//                    catch
//                    {
//                        tran.Rollback();
//                        throw;
//                    }
//                }
//            }
//        }

//        #endregion

//        public void ApplyDifferences(SystemInfo currentSystemInfo, List<Difference> diffs)
//        {
//            if (currentSystemInfo == null || diffs == null || diffs.Count == 0)
//                return;

//            // گروه‌بندی بر اساس EntityType
//            var groupedByEntity = diffs.GroupBy(d => d.EntityType).ToList();

//            using (var conn = new SqlConnection(_connectionString))
//            {
//                conn.Open();

//                foreach (var group in groupedByEntity)
//                {
//                    Type entityType = group.Key;
//                    string tableName = EntityMetadataHelper.GetTableName(entityType);

//                    // فرض بر اینکه همه رکوردها در این گروه SystemInfoRef یکسان دارند
//                    object foreignKeyValue = group.First().ForeignKeyValue;

//                    // --- ۱️⃣ expire رکوردهای قدیمی
//                    ExpireByForeignKey(tableName, foreignKeyValue);

//                    // --- ۲️⃣ گرفتن داده جدید از currentSystemInfo
//                    string propName = tableName; // مثلاً "GpuInfo" یا "RamModuleInfo"
//                    PropertyInfo prop = currentSystemInfo.GetType().GetProperty(propName);

//                    if (prop == null) continue;

//                    object newData = prop.GetValue(currentSystemInfo);

//                    if (newData == null) continue;

//                    // --- ۳️⃣ درج داده جدید
//                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
//                    {
//                        var list = ((IEnumerable)newData).Cast<object>().ToList();
//                        if (list.Count > 0)
//                            _insertHelper.InsertSimple(list);
//                    }
//                    else
//                    {
//                        _insertHelper.InsertSimple(newData, out _);
//                    }
//                }
//            }
//        }


//    }
//}


using SqlDataExtention.Attributes;
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

        #region Insert Simple
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
        #endregion

        #region Insert With Children (One-Level)
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
        #endregion

        #region Expire Helpers
        private void ExpireRow(object obj, SqlConnection conn, SqlTransaction tran)
        {
            if (obj == null) return;

            Type type = obj.GetType();
            string tableName = EntityMetadataHelper.GetTableName(type);
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            object keyValue = keyProp.GetValue(obj);
            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{keyColumn}] = @Key";
            var parameters = new[]
            {
                new SqlParameter("@Now", DateTime.Now),
                new SqlParameter("@Key", keyValue ?? DBNull.Value)
            };

            using (var cmd = new SqlCommand(query, conn, tran))
            {
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        public void ExpireByForeignKey(Type entityType, object foreignKeyValue)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (foreignKeyValue == null)
                throw new ArgumentNullException(nameof(foreignKeyValue));

            string tableName = EntityMetadataHelper.GetTableName(entityType);
            string fkColumn = "SystemInfoRef";

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{fkColumn}] = @Fk";
                    using (var cmd = new SqlCommand(query, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Fk", foreignKeyValue);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
            }
        }
        #endregion

        #region ApplyDifferences 
        // internal version that uses provided conn/tran (bulk expire)
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

        // جدید: ApplyDifferences - امن و براساس EntityType (یک تراکنش برای همه عملیات)
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

                    // اگر نه لیست نه تک شی پیدا شد، می‌توانیم تلاش کنیم پروپرتی‌ای که نام نوع را در نامش دارد بیابیم
                    // (اختیاری) - اما معمولاً بالا کافیست.
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

        #endregion





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

                        tran.Commit();
                        return success;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        // متد کمکی برای Insert یک رکورد با connection و transaction موجود
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
    }
}
