using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DashBoard.Data
{

    public class DataInsertHelper
    {
        private readonly DataHelper _dataHelper;

        public DataInsertHelper()
        {
            _dataHelper = new DataHelper();
        }

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
        VALUES ({string.Join(", ", parameters)})
    ";

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
            if (items == null || items.Count == 0)
                return results;

            foreach (var item in items)
            {
                bool success = InsertSimple(item, out var pk);
                results.Add((success, pk));
            }

            return results;
        }



        public bool InsertWithRelationsTransaction<T>(T obj, out object primaryKeyValue)
        {
            primaryKeyValue = null;

            using (var conn = _dataHelper.GetConnection())
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // درج رکورد اصلی و گرفتن کلید اصلی
                    primaryKeyValue = InsertSingle(obj, conn, transaction);

                    // پیدا کردن property کلید اصلی و مقداردهی آن به obj
                    var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T));
                    keyProp.SetValue(obj, Convert.ChangeType(primaryKeyValue, keyProp.PropertyType));

                    // پردازش بازگشتی فرزندها
                    InsertChildren(obj, conn, transaction);

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

        public List<(bool Success, object PrimaryKey)> InsertWithRelationsTransaction<T>(List<T> items)
        {
            var results = new List<(bool, object)>();
            if (items == null || items.Count == 0)
                return results;

            foreach (var item in items)
            {
                bool success = InsertWithRelationsTransaction(item, out var pk);
                results.Add((success, pk));
            }

            return results;
        }

        // متد کمکی: درج رکورد اصلی
        private object InsertSingle<T>(T obj, SqlConnection conn, SqlTransaction transaction)
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
        VALUES ({string.Join(", ", parameters)})
    ";

            var sqlParams = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(obj) ?? DBNull.Value)).ToArray();

            using (var cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddRange(sqlParams);
                return cmd.ExecuteScalar();
            }
        }

        // متد کمکی: درج بازگشتی فرزندها
        private void InsertChildren<T>(T obj, SqlConnection conn, SqlTransaction transaction)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => EntityMetadataHelper.IsIgnored(p))
                                 .ToList();

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                // نمونه کلاس
                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var childObj = prop.GetValue(obj);
                    if (childObj != null)
                    {
                        var parentKey = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T)).GetValue(obj);
                        var childFkProp = EntityMetadataHelper.GetForeignKeyProperty(propType);
                        childFkProp.SetValue(childObj, parentKey);

                        InsertChildrenRecursive(childObj, conn, transaction);
                    }
                }
                // لیست از نمونه‌ها
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

                            InsertChildrenRecursive(item, conn, transaction);
                        }
                    }
                }
            }
        }

        private void InsertChildrenRecursive(object obj, SqlConnection conn, SqlTransaction transaction)
        {
            //InsertSingle(obj, conn, transaction);
            //InsertChildren(obj, conn, transaction);
            Type type = obj.GetType();
            var method = typeof(DataInsertHelper).GetMethod("InsertSingle", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(type);
            var key = genericMethod.Invoke(this, new object[] { obj, conn, transaction });

            // مقدار کلید اصلی به obj ست شود
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            keyProp.SetValue(obj, key);

            InsertChildren(obj, conn, transaction);
        }

    }
}