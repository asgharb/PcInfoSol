using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using SqlDataExtention.Attributes;
using SqlDataExtention.Entity.Main;
using SqlDataExtention.Utils;
using static SqlDataExtention.Utils.SystemInfoComparer;

namespace SqlDataExtention.Data
{
    public class DataInsertUpdateHelper
    {
        private readonly DataHelper _dataHelper;

        public DataInsertUpdateHelper()
        {
            _dataHelper = new DataHelper();
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

            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToArray();

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

            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToArray();

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

        // فقط wrapper برای سازگاری با نام InsertRow
        private void InsertRow(object obj, SqlConnection conn, SqlTransaction tran)
        {
            InsertSingleDynamic(obj, conn, tran);
        }
        #endregion

        #region Update or Insert Children Only
        public void UpdateOrInsertChildrenOnly(object dbParent, object newParent)
        {
            if (dbParent == null || newParent == null)
                throw new ArgumentNullException();

            using (var conn = _dataHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    var childProps = dbParent.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => Attribute.IsDefined(p, typeof(IgnoreAttribute)));

                    foreach (var prop in childProps)
                    {
                        Type propType = prop.PropertyType;

                        if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                        {
                            var dbChild = prop.GetValue(dbParent);
                            var newChild = prop.GetValue(newParent);
                            if (newChild == null) continue;

                            if (dbChild != null)
                            {
                                var diffs = SystemInfoComparer.CompareSystemInfo(newChild, dbChild);
                                if (diffs.Count > 0)
                                {
                                    ExpireRow(dbChild, conn, tran);
                                    InsertSingleDynamic(newChild, conn, tran);
                                }
                            }
                            else
                            {
                                InsertSingleDynamic(newChild, conn, tran);
                            }
                        }
                        else if (propType.IsGenericType)
                        {
                            var dbList = (prop.GetValue(dbParent) as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
                            var newList = (prop.GetValue(newParent) as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

                            int count = Math.Min(dbList.Count, newList.Count);
                            for (int i = 0; i < count; i++)
                            {
                                var diffs = SystemInfoComparer.CompareSystemInfo(newList[i], dbList[i]);
                                if (diffs.Count > 0)
                                {
                                    ExpireRow(dbList[i], conn, tran);
                                    InsertSingleDynamic(newList[i], conn, tran);
                                }
                            }

                            for (int i = count; i < newList.Count; i++)
                            {
                                InsertSingleDynamic(newList[i], conn, tran);
                            }
                        }
                    }

                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
        #endregion

        #region Expire Row
        //private void ExpireRow(object obj, SqlConnection conn, SqlTransaction tran)
        //{
        //    Type type = obj.GetType();
        //    string tableName = EntityMetadataHelper.GetTableName(type);
        //    var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
        //    object keyValue = keyProp.GetValue(obj);
        //    string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);

        //    string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{keyColumn}] = @Key";
        //    var parameters = new[]
        //    {
        //        new SqlParameter("@Now", DateTime.Now),
        //        new SqlParameter("@Key", keyValue)
        //    };

        //    using (var cmd = new SqlCommand(query, conn, tran))
        //    {
        //        cmd.Parameters.AddRange(parameters);
        //        cmd.ExecuteNonQuery();
        //    }
        //}
        //


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

        //private void ExpireByForeignKey(string tableName, object foreignKeyValue, SqlConnection conn, SqlTransaction tran)
        //{
        //    if (string.IsNullOrEmpty(tableName))
        //        throw new ArgumentNullException(nameof(tableName));
        //    if (foreignKeyValue == null)
        //        throw new ArgumentNullException(nameof(foreignKeyValue));
        //    if (conn == null)
        //        throw new ArgumentNullException(nameof(conn));
        //    if (tran == null)
        //        throw new ArgumentNullException(nameof(tran));

        //    // فرض بر اینه که نام ستون foreign key همیشه "SystemInfoRef" هست
        //    string fkColumn = "SystemInfoRef";

        //    string query = $"UPDATE [{tableName}] SET [ExpireDate] = @Now WHERE [{fkColumn}] = @Fk";

        //    using (var cmd = new SqlCommand(query, conn, tran))
        //    {
        //        cmd.Parameters.AddWithValue("@Now", DateTime.Now);
        //        cmd.Parameters.AddWithValue("@Fk", foreignKeyValue);
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        public void ExpireByForeignKey(Type entityType, object foreignKeyValue)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (foreignKeyValue == null)
                throw new ArgumentNullException(nameof(foreignKeyValue));

            // گرفتن نام جدول از نوع
            string tableName = EntityMetadataHelper.GetTableName(entityType);
            if (string.IsNullOrEmpty(tableName))
                throw new Exception($"Table name for type {entityType.Name} not found.");

            string fkColumn = "SystemInfoRef"; // فرض ثابت

            using (var conn = _dataHelper.GetConnectionClosed())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
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
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion






        public void SyncSingleTableByDiff(SystemInfo currentSystemInfo, SystemInfo dbSystemInfo, List<Difference> diffs)
        {
            if (currentSystemInfo == null || dbSystemInfo == null || diffs == null || diffs.Count == 0)
                return;

            // گرفتن نوع جدول مورد اختلاف (فرض: همه diffs مربوط به یک جدول هستند)
            Type entityType = diffs.First().EntityType;

            // 1️⃣ Expire همه رکوردهای این جدول بر اساس ForeignKey (SystemInfoRef)
            object foreignKeyValue = typeof(SystemInfo).GetProperty("SystemInfoID").GetValue(currentSystemInfo);
            ExpireByForeignKey(entityType, foreignKeyValue);

            // 2️⃣ Insert داده‌های جاری همان جدول
            PropertyInfo prop = typeof(SystemInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericArguments()[0] == entityType);

            if (prop != null)
            {
                var list = prop.GetValue(currentSystemInfo) as IEnumerable;
                if (list != null)
                {
                    var items = list.Cast<object>().ToList();
                    if (items.Count > 0)
                    {
                        // استفاده از InsertSimple
                        var method = typeof(DataInsertUpdateHelper)
                            .GetMethod(nameof(InsertSimple), new Type[] { typeof(List<>).MakeGenericType(entityType) })
                            .MakeGenericMethod(entityType);

                        method.Invoke(this, new object[] { items });
                    }
                }
            }
            else
            {
                // تک شی (مثل CpuInfo یا RamSummaryInfo)
                var singleProp = typeof(SystemInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.PropertyType == entityType);

                if (singleProp != null)
                {
                    var obj = singleProp.GetValue(currentSystemInfo);
                    if (obj != null)
                    {
                        // استفاده از InsertSimple تک شی
                        var method = typeof(DataInsertUpdateHelper)
                            .GetMethod(nameof(InsertSimple), new Type[] { entityType.MakeByRefType(), typeof(object).MakeByRefType() })
                            .MakeGenericMethod(entityType);

                        object pk = null;
                        method.Invoke(this, new object[] { obj, pk });
                    }
                }
            }
        }

        private object GetObjectFromCurrentPath(object rootObj, string path)
        {
            if (rootObj == null || string.IsNullOrEmpty(path))
                return null;

            object current = rootObj;
            string[] parts = path.Split('.');

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                string propName = part;
                int? index = null;

                // بررسی وجود index
                if (part.Contains("[") && part.EndsWith("]"))
                {
                    int start = part.IndexOf("[");
                    int end = part.IndexOf("]");
                    propName = part.Substring(0, start);
                    if (int.TryParse(part.Substring(start + 1, end - start - 1), out int idx))
                        index = idx;
                }

                // گرفتن property از current (که کلاس است)
                PropertyInfo prop = current.GetType().GetProperty(
                    propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
                );
                if (prop == null)
                    return null;

                current = prop.GetValue(current);

                // اگر index مشخص شده، روی current که باید List باشد اعمال کن
                if (index.HasValue)
                {
                    if (current is IEnumerable list && !(current is string))
                    {
                        current = list.Cast<object>().ElementAtOrDefault(index.Value);
                    }
                    else
                    {
                        return null; // index روی non-list → خطا
                    }
                }
            }

            return current;
        }

        //private PropertyInfo GetPropertyRecursive(Type type, string propName)
        //{
        //    while (type != null)
        //    {
        //        var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //        if (prop != null) return prop;
        //        type = type.BaseType;
        //    }
        //    return null;
        //}


    }
}
