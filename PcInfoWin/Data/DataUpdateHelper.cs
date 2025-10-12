using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using static PcInfoWin.Utils.SystemInfoComparer;

namespace PcInfoWin.Data
{
    public class DataUpdateHelper
    {
        private readonly DataHelper _dataHelper;

        public DataUpdateHelper()
        {
            _dataHelper = new DataHelper();
        }

        #region 1️⃣ Update ساده (UpdateSingle)
        public bool UpdateSingle<T>(T obj)
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);

            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);
            object keyValue = keyProp.GetValue(obj);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p) && p != keyProp)
                            .ToList();

            if (!props.Any()) return false;

            var setClauses = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}] = @{p.Name}");
            string query = $"UPDATE [{tableName}] SET {string.Join(", ", setClauses)} WHERE [{keyColumn}] = @Key";

            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToList();
            sqlParams.Add(new SqlParameter("@Key", keyValue));

            try
            {
                _dataHelper.ExecuteNonQuery(query, sqlParams.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 2️⃣ Update بازگشتی با روابط (UpdateWithRelations)
        public bool UpdateWithRelations<T>(T obj)
        {
            using (var conn = _dataHelper.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    UpdateSingleTransactional(obj, conn, transaction);
                    UpdateChildren(obj, conn, transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        private void UpdateSingleTransactional<T>(T obj, SqlConnection conn, SqlTransaction transaction)
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);

            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            string keyColumn = EntityMetadataHelper.GetColumnName(keyProp);
            object keyValue = keyProp.GetValue(obj);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !EntityMetadataHelper.IsIgnored(p) && !EntityMetadataHelper.IsDbGenerated(p) && p != keyProp)
                            .ToList();

            if (!props.Any()) return;

            var setClauses = props.Select(p => $"[{EntityMetadataHelper.GetColumnName(p)}] = @{p.Name}");
            string query = $"UPDATE [{tableName}] SET {string.Join(", ", setClauses)} WHERE [{keyColumn}] = @Key";

            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToList();
            sqlParams.Add(new SqlParameter("@Key", keyValue));

            using (var cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddRange(sqlParams.ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateChildren<T>(T obj, SqlConnection conn, SqlTransaction transaction)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => EntityMetadataHelper.IsIgnored(p))
                                 .ToList();

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var childObj = prop.GetValue(obj);
                    if (childObj != null)
                    {
                        var parentKey = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T)).GetValue(obj);
                        var childFkProp = EntityMetadataHelper.GetForeignKeyProperty(propType);
                        childFkProp.SetValue(childObj, parentKey);

                        UpdateChildrenRecursive(childObj, conn, transaction);
                    }
                }
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    var list = prop.GetValue(obj) as IEnumerable;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var parentKey = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T)).GetValue(obj);
                            var childFkProp = EntityMetadataHelper.GetForeignKeyProperty(itemType);
                            childFkProp.SetValue(item, parentKey);

                            UpdateChildrenRecursive(item, conn, transaction);
                        }
                    }
                }
            }
        }

        private void UpdateChildrenRecursive(object obj, SqlConnection conn, SqlTransaction transaction)
        {
            Type type = obj.GetType();
            UpdateSingleTransactional(obj, conn, transaction);
            UpdateChildren(obj, conn, transaction);
        }
        #endregion

        #region 3️⃣ Update شرطی (UpdateWhere)
        // 1️⃣ UpdateWhere با Generic
        public int UpdateWhere<T>(string setClause, string whereClause, params SqlParameter[] parameters)
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"UPDATE [{tableName}] SET {setClause} WHERE {whereClause}";
            return _dataHelper.ExecuteNonQuery(query, parameters);
        }

        // 2️⃣ UpdateColumnByForeignKey با Generic
        public int UpdateColumnByForeignKey<T>(string columnName, object newValue, object foreignKeyValue)
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(typeof(T));
            string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

            string query = $"UPDATE [{tableName}] SET [{columnName}] = @newValue WHERE [{fkColumn}] = @fkValue";

            var parameters = new[]
            {
        new SqlParameter("@newValue", newValue ?? DBNull.Value),
        new SqlParameter("@fkValue", foreignKeyValue ?? DBNull.Value)
    };

            return _dataHelper.ExecuteNonQuery(query, parameters);
        }

        public int UpdateByForeignKey<T>(string columnName, object newValue, object foreignKeyValue)
        {
            Type type = typeof(T);

            // استخراج نام جدول از Attribute [Table]
            string tableName = EntityMetadataHelper.GetTableName(type);

            // پیدا کردن خاصیت کلید خارجی، چه در کلاس فعلی چه در BaseEntity
            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(type);

            // گرفتن نام فیلد در دیتابیس (از Attribute [Column])
            string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

            // ساخت کوئری
            string query = $"UPDATE [{tableName}] SET [{columnName}] = @newValue WHERE [{fkColumn}] = @fkValue";

            // پارامترها
            var parameters = new[]
            {
        new SqlParameter("@newValue", newValue ?? DBNull.Value),
        new SqlParameter("@fkValue", foreignKeyValue ?? DBNull.Value)
    };

            // اجرای دستور
            return _dataHelper.ExecuteNonQuery(query, parameters);
        }



        //    public int UpdateByForeignKey<T>(string columnName, object newValue, object foreignKeyValue)
        //    {
        //        Type type = typeof(T);

        //        // استخراج نام جدول
        //        string tableName = EntityMetadataHelper.GetTableName(type);

        //        // جستجوی اولین خاصیت با ForeignKeyAttribute در خود کلاس یا کلاس پایه
        //        var fkProp = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
        //            .FirstOrDefault(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute)));

        //        if (fkProp == null)
        //            throw new Exception($"ForeignKeyAttribute not found in class {type.Name} or its base types.");

        //        string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

        //        string query = $"UPDATE [{tableName}] SET [{columnName}] = @newValue WHERE [{fkColumn}] = @fkValue";

        //        var parameters = new[]
        //        {
        //    new SqlParameter("@newValue", newValue ?? DBNull.Value),
        //    new SqlParameter("@fkValue", foreignKeyValue ?? DBNull.Value)
        //};

        //        return _dataHelper.ExecuteNonQuery(query, parameters);
        //    }

    //    public int UpdateByForeignKey<T>(
    //string columnName,
    //object newValue,
    //object foreignKeyValue,
    //Type parentType = null)
    //    {
    //        Type type = typeof(T);
    //        string tableName = EntityMetadataHelper.GetTableName(type);
    //        PropertyInfo fkProp = null;

    //        // 🧩 حالت ۱: اگر parentType داده شده (تشخیص ForeignKey خاص برای والد)
    //        if (parentType != null)
    //        {
    //            fkProp = EntityMetadataHelper
    //                .GetForeignKeyPropertyForParent(
    //                    type.GetProperty($"{parentType.Name}List") ?? type.GetProperty($"{parentType.Name}"),
    //                    parentType);
    //        }

    //        // 🧩 حالت ۲: اگر ForeignKey عمومی در کلاس پایه وجود دارد
    //        if (fkProp == null)
    //        {
    //            fkProp = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
    //                .FirstOrDefault(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute)));
    //        }

    //        // 🧩 حالت ۳: اگر هنوز پیدا نشد و کلاس پایه دارد (مثلاً BaseEntity)
    //        if (fkProp == null && type.BaseType != null)
    //        {
    //            fkProp = type.BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                .FirstOrDefault(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute)));
    //        }

    //        if (fkProp == null)
    //            throw new Exception($"ForeignKeyAttribute not found in {type.Name} or its base types.");

    //        string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

    //        string query = $"UPDATE [{tableName}] SET [{columnName}] = @newValue WHERE [{fkColumn}] = @fkValue";

    //        var parameters = new[]
    //        {
    //    new SqlParameter("@newValue", newValue ?? DBNull.Value),
    //    new SqlParameter("@fkValue", foreignKeyValue ?? DBNull.Value)
    //};

    //        return _dataHelper.ExecuteNonQuery(query, parameters);
    //    }


        #endregion
        public bool ExpireSystemInfoAndRelations(int systemInfoId)
        {
            using (var conn = _dataHelper.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    DateTime now = DateTime.Now;

                    // --- 1. آپدیت جدول اصلی SystemInfo ---
                    string mainTable = EntityMetadataHelper.GetTableName(typeof(SystemInfo));
                    string updateMainQuery = $"UPDATE [{mainTable}] SET [ExpireDate] = @now WHERE [SystemInfoID] = @id";

                    _dataHelper.ExecuteNonQuery(updateMainQuery,
                        new SqlParameter("@now", now),
                        new SqlParameter("@id", systemInfoId));

                    // --- 2. یافتن propertyهای Ignore (یعنی کلاس‌های وابسته) ---
                    var props = typeof(SystemInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(p => Attribute.IsDefined(p, typeof(IgnoreAttribute)))
                                                  .ToList();

                    foreach (var prop in props)
                    {
                        Type propType = prop.PropertyType;

                        // --- حالت تکی (مثلاً CpuInfo، GpuInfo، MotherboardInfo) ---
                        if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                        {
                            string tableName = EntityMetadataHelper.GetTableName(propType);
                            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(propType);
                            string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

                            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @now WHERE [{fkColumn}] = @id";
                            _dataHelper.ExecuteNonQuery(query,
                                new SqlParameter("@now", now),
                                new SqlParameter("@id", systemInfoId));
                        }
                        // --- حالت لیستی (مثلاً List<DiskInfo> یا List<RamModuleInfo>) ---
                        else if (propType.IsGenericType)
                        {
                            Type itemType = propType.GetGenericArguments()[0];
                            string tableName = EntityMetadataHelper.GetTableName(itemType);
                            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(itemType);
                            string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

                            string query = $"UPDATE [{tableName}] SET [ExpireDate] = @now WHERE [{fkColumn}] = @id";
                            _dataHelper.ExecuteNonQuery(query,
                                new SqlParameter("@now", now),
                                new SqlParameter("@id", systemInfoId));
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void ApplySystemInfoDifferences(List<Difference> differences, int systemInfoId)
        {
            if (differences == null || differences.Count == 0)
                return;

            using (var conn = new SqlConnection(ConnctionString.GetConnctionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var diff in differences)
                        {
                            // تشخیص نوع موجودیت (entity)

    
                            int index = diff.Path.IndexOf('[');
                            if (index >= 0)
                                diff.Path = diff.Path.Substring(0, index);

                            Type entityType = GetEntityTypeFromPath(diff.Path);
                            string tableName = EntityMetadataHelper.GetTableName(entityType);

                            // تشخیص نام ستون
                            string columnName = diff.Property;
                            if (string.IsNullOrEmpty(columnName))
                                continue;

                            // تشخیص کلید اصلی جدول
                            var keyProp = entityType.GetProperties()
                                .FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
                            string keyColumn = keyProp != null
                                ? EntityMetadataHelper.GetColumnName(keyProp)
                                : "ID";

                            var keyValue = diff.PrimaryKeyValue;

                            // اگر کلید اصلی null بود یعنی این سطر هنوز درج نشده ➜ INSERT
                            if (keyValue == null || Convert.ToInt32(keyValue) == 0)
                            {
                                // 🔹 درج رکورد جدید
                                string sqlInsert = $@"
                            INSERT INTO [{tableName}] ([SystemInfoRef], [{columnName}])
                            VALUES (@SystemInfoRef, @Value);";

                                using (var cmd = new SqlCommand(sqlInsert, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@SystemInfoRef", systemInfoId);
                                    cmd.Parameters.AddWithValue("@Value", diff.Value2 ?? (object)DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // 🔹 بررسی اینکه آیا رکورد وجود دارد
                                string checkSql = $@"
                            SELECT COUNT(*) FROM [{tableName}]
                            WHERE [{keyColumn}] = @Key";
                                bool exists;
                                using (var checkCmd = new SqlCommand(checkSql, conn, tran))
                                {
                                    checkCmd.Parameters.AddWithValue("@Key", keyValue);
                                    exists = (int)checkCmd.ExecuteScalar() > 0;
                                }

                                if (exists)
                                {
                                    // 🔹 بروزرسانی رکورد موجود
                                    string sqlUpdate = $@"
                                UPDATE [{tableName}]
                                SET [{columnName}] = @Value
                                WHERE [{keyColumn}] = @Key";

                                    using (var cmd = new SqlCommand(sqlUpdate, conn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@Value", diff.Value1 ?? (object)DBNull.Value);
                                        cmd.Parameters.AddWithValue("@Key", keyValue);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    // 🔹 اگر وجود ندارد ➜ درج جدید
                                    string sqlInsert = $@"
                                INSERT INTO [{tableName}] ([SystemInfoRef], [{columnName}])
                                VALUES (@SystemInfoRef, @Value);";

                                    using (var cmd = new SqlCommand(sqlInsert, conn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@SystemInfoRef", systemInfoId);
                                        cmd.Parameters.AddWithValue("@Value", diff.Value2 ?? (object)DBNull.Value);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw new Exception("خطا در به‌روزرسانی تفاوت‌ها: " + ex.Message, ex);
                    }
                }
            }
        }

        //private Type GetEntityTypeFromPath(string path)
        //{
        //    if (string.IsNullOrWhiteSpace(path))
        //        return typeof(SystemInfo); // حالت پیش‌فرض

        //    // مثال path: "SystemInfo.RamModules[0].Size"
        //    string[] parts = path.Split('.');

        //    // اگر شامل نقطه هست، دومین بخش معمولاً نام کلاس زیرمجموعه است
        //    string entityName = parts.Length > 1 ? parts[1].Split('[')[0] : parts[0];

        //    // در اسمبلی فعلی دنبال کلاسی با این نام بگرد
        //    var type = typeof(SystemInfo).Assembly
        //        .GetTypes()
        //        .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        //    // اگر پیدا نکرد، همون SystemInfo رو برگردون
        //    return type ?? typeof(SystemInfo);
        //}

        //private Type GetEntityTypeFromPath(string path)
        //{
        //    if (string.IsNullOrWhiteSpace(path))
        //        return typeof(SystemInfo); // حالت پیش‌فرض

        //    // مثال path: "SystemInfo.gpuInfo.Name"
        //    string[] parts = path.Split('.');

        //    // اگر فقط یک سطح داریم
        //    if (parts.Length < 2)
        //        return typeof(SystemInfo);

        //    // نام property در کلاس والد (SystemInfo)
        //    string propName = parts[1].Split('[')[0];

        //    // 1️⃣ بررسی مستقیم property در کلاس SystemInfo
        //    var prop = typeof(SystemInfo).GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        //    if (prop != null)
        //    {
        //        Type propType = prop.PropertyType;

        //        // اگر لیست هست (List<T>)، نوع درونی T را برگردان
        //        if (propType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propType))
        //            return propType.GetGenericArguments()[0];

        //        return propType;
        //    }

        //    // 2️⃣ در غیر این صورت، سعی کن از روی نام کلاس در اسمبلی پیدا کنی
        //    string entityName = char.ToUpper(propName[0]) + propName.Substring(1); // gpuInfo → GpuInfo

        //    var type = typeof(SystemInfo).Assembly
        //        .GetTypes()
        //        .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        //    // اگر پیدا نکرد، SystemInfo برگردان
        //    return type ?? typeof(SystemInfo);
        //}


        private Type GetEntityTypeFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return typeof(SystemInfo);

            // مثال path: "SystemInfo.gpuInfo.Name"
            string[] parts = path.Split('.');

            // بخش دوم معمولاً نام پراپرتی زیرمجموعه است
            string entityName = parts.Length > 1 ? parts[1].Split('[')[0] : parts[0];
            entityName = entityName.Trim();

            if (string.IsNullOrEmpty(entityName))
                return typeof(SystemInfo);

            // حرف اول بزرگ برای هماهنگی با نام کلاس‌ها
            entityName = char.ToUpperInvariant(entityName[0]) + entityName.Substring(1);

            var allTypes = typeof(SystemInfo).Assembly.GetTypes();

            // جستجوی مستقیم (case-insensitive)
            var type = allTypes.FirstOrDefault(t =>
                t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            // اگر پیدا نشد، سعی کن "Info" را اضافه یا حذف کنی
            if (type == null)
            {
                if (!entityName.EndsWith("Info", StringComparison.OrdinalIgnoreCase))
                {
                    string altName = entityName + "Info";
                    type = allTypes.FirstOrDefault(t =>
                        t.Name.Equals(altName, StringComparison.OrdinalIgnoreCase));
                }
                else if (entityName.EndsWith("Info", StringComparison.OrdinalIgnoreCase))
                {
                    // حذف "Info" بدون حساسیت به حروف
                    int infoIndex = entityName.LastIndexOf("Info", StringComparison.OrdinalIgnoreCase);
                    string altName = entityName.Remove(infoIndex, 4); // حذف کلمه Info
                    type = allTypes.FirstOrDefault(t =>
                        t.Name.Equals(altName, StringComparison.OrdinalIgnoreCase));
                }
            }

            return type ?? typeof(SystemInfo);
        }



    }
}
