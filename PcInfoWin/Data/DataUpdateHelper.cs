using PcInfoWin.Attributes;
using PcInfoWin.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Data
{
    public class DataUpdateHelper
    {
        private readonly DataHelper _dataHelper;

        public DataUpdateHelper(DataHelper dataHelper)
        {
            _dataHelper = dataHelper;
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
        public int UpdateWhere(string tableName, string setClause, string whereClause, params SqlParameter[] parameters)
        {
            string query = $"UPDATE [{tableName}] SET {setClause} WHERE {whereClause}";
            return _dataHelper.ExecuteNonQuery(query, parameters);
        }

        public int UpdateColumnByForeignKey(Type classType, string columnName, object newValue, object foreignKeyValue)
        {
            string tableName = EntityMetadataHelper.GetTableName(classType);
            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(classType);
            string fkColumn = EntityMetadataHelper.GetColumnName(fkProp);

            string query = $"UPDATE [{tableName}] SET [{columnName}] = @newValue WHERE [{fkColumn}] = @fkValue";

            var parameters = new[]
            {
        new SqlParameter("@newValue", newValue ?? DBNull.Value),
        new SqlParameter("@fkValue", foreignKeyValue ?? DBNull.Value)
    };

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

    }
}
