using DashBoard.Attributes;
using DashBoard.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static DashBoard.Utils.SystemInfoComparer;

namespace DashBoard.Data
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

        public bool UpdateChildWithHistory<TChild>(
            int parentId,
            string columnNameToUpdate,
            object newValue
        ) where TChild : BaseEntity, new()
        {
            using (var conn = _dataHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    DateTime now = DateTime.Now;
                    Type childType = typeof(TChild);
                    string tableName = EntityMetadataHelper.GetTableName(childType);

                    // پیدا کردن ForeignKey به والد
                    var fkProp = EntityMetadataHelper.GetForeignKeyProperty(childType);
                    string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

                    // پیدا کردن Property کلید اصلی و نام ستون آن (برای حذف از INSERT)
                    var pkProp = EntityMetadataHelper.GetPrimaryKeyProperty(childType);
                    string pkColumn = EntityMetadataHelper.GetColumnName(pkProp);

                    // آماده‌سازی نگاشت نام ستون -> PropertyInfo برای تشخیص DbGenerated و نام ستون
                    var props = childType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var columnToProp = props.ToDictionary(
                        p => EntityMetadataHelper.GetColumnName(p),
                        p => p,
                        StringComparer.OrdinalIgnoreCase);

                    // دریافت ردیف(های) فعلی (ExpireDate IS NULL)
                    var existingRows = new List<Dictionary<string, object>>();
                    using (var cmd = new SqlCommand($"SELECT * FROM [{tableName}] WHERE [{fkColumn}] = @parentId AND [ExpireDate] IS NULL", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@parentId", parentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                existingRows.Add(row);
                            }
                        }
                    }

                    // اگر ردیف فعالی وجود نداشت، فقط یک ردیف جدید مینیمال درج کن
                    if (!existingRows.Any())
                    {
                        // سعی می‌کنیم حداقل ستون‌های لازم را درج کنیم: FK، ستون موردنظر، InsertDate، ExpireDate(NULL)
                        var insertCols = new List<string> { $"[{fkColumn}]", $"[{columnNameToUpdate}]", "[InsertDate]", "[ExpireDate]" };
                        var insertParams = new List<string> { "@fk", "@val", "@insertDate", "@expireDate" };
                        using (var cmd = new SqlCommand($"INSERT INTO [{tableName}] ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)})", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@fk", parentId);
                            cmd.Parameters.AddWithValue("@val", newValue ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@insertDate", now);
                            cmd.Parameters.AddWithValue("@expireDate", DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return true;
                    }

                    // 1) Expire کردن ردیف‌های فعلی
                    using (var cmd = new SqlCommand($"UPDATE [{tableName}] SET [ExpireDate] = @now WHERE [{fkColumn}] = @parentId AND [ExpireDate] IS NULL", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@now", now);
                        cmd.Parameters.AddWithValue("@parentId", parentId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2) برای هر ردیف موجود یک ردیف جدید درج کن، اما ستون PK و ستون‌های DbGenerated را حذف کن
                    foreach (var row in existingRows)
                    {
                        var columns = new List<string>();
                        var parameters = new List<string>();
                        var sqlParams = new List<SqlParameter>();

                        foreach (var kv in row)
                        {
                            string colName = kv.Key;

                            // همیشه از درج InsertDate و ExpireDate بخاطر ست کردن جداگانه صرف‌نظر کن
                            if (string.Equals(colName, "InsertDate", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(colName, "ExpireDate", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // حذف PK
                            if (string.Equals(colName, pkColumn, StringComparison.OrdinalIgnoreCase))
                                continue;

                            // اگر ستون متناظر با Property وجود دارد، بررسی کن که آیا DbGenerated است؛ در این صورت حذف شود
                            if (columnToProp.TryGetValue(colName, out var propInfo))
                            {
                                if (EntityMetadataHelper.IsDbGenerated(propInfo))
                                    continue; // حذف ستون‌هایی که DbGenerated هستند
                            }

                            // حالا ستون را اضافه کن؛ اگر ستون همان ستون موردآپدیت است مقدار جدید را بگذار
                            columns.Add($"[{colName}]");
                            parameters.Add($"@{colName}");
                            object val = string.Equals(colName, columnNameToUpdate, StringComparison.OrdinalIgnoreCase) ? newValue ?? (object)DBNull.Value : kv.Value ?? (object)DBNull.Value;
                            sqlParams.Add(new SqlParameter($"@{colName}", val));
                        }

                        // مطمئن شو ستون FK حتما وجود دارد (پایگاه ممکن است آن را در ردیف داشته باشد)
                        if (!columns.Any(c => c.Equals($"[{fkColumn}]", StringComparison.OrdinalIgnoreCase)))
                        {
                            columns.Add($"[{fkColumn}]");
                            parameters.Add($"@{fkColumn}");
                            sqlParams.Add(new SqlParameter($"@{fkColumn}", parentId));
                        }

                        // اضافه کردن InsertDate و ExpireDate
                        columns.Add("[InsertDate]");
                        parameters.Add("@InsertDate");
                        sqlParams.Add(new SqlParameter("@InsertDate", now));

                        columns.Add("[ExpireDate]");
                        parameters.Add("@ExpireDate");
                        sqlParams.Add(new SqlParameter("@ExpireDate", DBNull.Value));

                        string insertSql = $"INSERT INTO [{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";

                        using (var cmd = new SqlCommand(insertSql, conn, tran))
                        {
                            cmd.Parameters.AddRange(sqlParams.ToArray());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                    return true;
                }
                catch
                {
                    tran.Rollback();
                    throw;
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




        public bool UpdatePcCodeWithHistory(int systemInfoId, string newPcCode)
        {
            using (var conn = _dataHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    DateTime now = DateTime.Now;

                    // 1️⃣ دریافت مقدار قبلی PcCode
                    string selectSql = @"
                SELECT pcCode
                FROM [SystemInfo]
                WHERE [SystemInfoID] = @id";

                    string oldPcCode;
                    using (var cmd = new SqlCommand(selectSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", systemInfoId);
                        oldPcCode = cmd.ExecuteScalar()?.ToString();
                    }

                    // 2️⃣ اگر مقدار تغییر کرده، تاریخچه PcCodeInfo را بروزرسانی کن
                    if (!string.Equals(oldPcCode, newPcCode, StringComparison.Ordinal))
                    {
                        // 2a️⃣ منقضی کردن سطر قبلی در PcCodeInfo
                        string expireSql = @"
                    UPDATE [PcCodeInfo]
                    SET [ExpireDate] = @now
                    WHERE [SystemInfoRef] = @id AND [PcCode] = @oldPcCode AND [ExpireDate] IS NULL";

                        using (var cmd = new SqlCommand(expireSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@now", now);
                            cmd.Parameters.AddWithValue("@id", systemInfoId);
                            cmd.Parameters.AddWithValue("@oldPcCode", oldPcCode);
                            cmd.ExecuteNonQuery();
                        }

                        // 2b️⃣ درج سطر جدید در PcCodeInfo
                        string insertSql = @"
                    INSERT INTO [PcCodeInfo] ([SystemInfoRef], [PcCode], [InsertDate])
                    VALUES (@id, @newPcCode, @now)";

                        using (var cmd = new SqlCommand(insertSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", systemInfoId);
                            cmd.Parameters.AddWithValue("@newPcCode", newPcCode);
                            cmd.Parameters.AddWithValue("@now", now);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 3️⃣ بروزرسانی جدول اصلی SystemInfo
                    string updateMainSql = @"
                UPDATE [SystemInfo]
                SET [PcCode] = @newPcCode
                WHERE [SystemInfoID] = @id";

                    using (var cmd = new SqlCommand(updateMainSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", systemInfoId);
                        cmd.Parameters.AddWithValue("@newPcCode", newPcCode);
                        cmd.ExecuteNonQuery();
                    }

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


        public bool UpdateChildFieldWithHistory<TChild>(
    int systemInfoId,
    string propertyName,
    object newValue
) where TChild : BaseEntity, new()
        {
            using (var conn = _dataHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    DateTime now = DateTime.Now;
                    string tableName = EntityMetadataHelper.GetTableName(typeof(TChild));

                    // 1️⃣ پیدا کردن پراپرتی مورد نظر
                    var prop = typeof(TChild).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null)
                        throw new Exception($"Property '{propertyName}' not found in {typeof(TChild).Name}");

                    string columnName = EntityMetadataHelper.GetColumnName(prop);

                    // 2️⃣ Expire مقادیر قبلی
                    string expireSql = $@"
                UPDATE [{tableName}]
                SET [ExpireDate] = @now
                WHERE [SystemInfoRef] = @systemInfoId AND [{columnName}] = @oldValue AND [ExpireDate] IS NULL";

                    object oldValue;
                    using (var cmd = new SqlCommand($"SELECT TOP 1 [{columnName}] FROM [{tableName}] WHERE [SystemInfoRef] = @systemInfoId AND [ExpireDate] IS NULL ORDER BY [InsertDate] DESC", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@systemInfoId", systemInfoId);
                        oldValue = cmd.ExecuteScalar();
                    }

                    if (oldValue != null && !Equals(oldValue, newValue))
                    {
                        using (var cmd = new SqlCommand(expireSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@now", now);
                            cmd.Parameters.AddWithValue("@systemInfoId", systemInfoId);
                            cmd.Parameters.AddWithValue("@oldValue", oldValue);
                            cmd.ExecuteNonQuery();
                        }

                        // 3️⃣ درج سطر جدید
                        string insertSql = $@"
                    INSERT INTO [{tableName}] ([SystemInfoRef], [{columnName}], [InsertDate])
                    VALUES (@systemInfoId, @newValue, @now)";

                        using (var cmd = new SqlCommand(insertSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@systemInfoId", systemInfoId);
                            cmd.Parameters.AddWithValue("@newValue", newValue ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@now", now);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                    return true;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }


    }
}
