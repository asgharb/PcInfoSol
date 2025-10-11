using PcInfoWin.Attributes;
using PcInfoWin.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;


namespace PcInfoWin.Data
{
    using PcInfoWin.Entity.Main;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Reflection;

    public class DataSelectHelper
    {
        private readonly DataHelper _dataHelper;

        public DataSelectHelper()
        {
            _dataHelper = new DataHelper();
        }

        #region پایه: واکشی ساده

        public List<T> SelectAll<T>() where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT * FROM [{tableName}]";

            var dt = _dataHelper.ExecuteQuery(query);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public T SelectByPrimaryKey<T>(object keyValue) where T : new()
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            string columnName = EntityMetadataHelper.GetColumnName(keyProp);

            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val";
            var param = new SqlParameter("@val", keyValue);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt).FirstOrDefault();
        }

        public List<T> SelectByForeignKey<T>(object foreignValue) where T : new()
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(type);
            string columnName = EntityMetadataHelper.GetColumnName(fkProp);

            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val";
            var param = new SqlParameter("@val", foreignValue);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt);
        }

        #endregion

        #region واکشی بازگشتی روابط [Ignore]

        //public T SelectWithRelationsByPrimaryKey<T>(object keyValue) where T : new()
        //{
        //    // ابتدا شی اصلی را انتخاب می‌کنیم
        //    var mainObj = SelectByPrimaryKey<T>(keyValue);
        //    if (mainObj == null) return default;

        //    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    foreach (var prop in props)
        //    {
        //        // فقط فیلدهایی که [Ignore] دارند یعنی نمونه یا لیست کلاس دیگر
        //        if (!EntityMetadataHelper.IsIgnored(prop))
        //            continue;

        //        Type propType = prop.PropertyType;

        //        // نمونه کلاس دیگر (تک‌تایی)
        //        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
        //        {
        //            try
        //            {
        //                // پیدا کردن foreign key در والد که به این child اشاره می‌کند
        //                var parentFkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, typeof(T));
        //                if (parentFkProp == null)
        //                    continue;

        //                var foreignKeyValue = parentFkProp.GetValue(mainObj);
        //                if (foreignKeyValue == null)
        //                    continue;

        //                // فراخوانی بازگشتی
        //                var method = typeof(DataSelectHelper)
        //                    .GetMethod(nameof(SelectWithRelationsByPrimaryKey))
        //                    .MakeGenericMethod(propType);

        //                var childObj = method.Invoke(this, new object[] { foreignKeyValue });
        //                prop.SetValue(mainObj, childObj);
        //            }
        //            catch
        //            {
        //                // اگر مشکلی بود نادیده گرفته شود
        //            }
        //        }
        //        // لیست از نمونه‌ها
        //        else if (propType.IsGenericType)
        //        {
        //            Type itemType = propType.GetGenericArguments()[0];
        //            try
        //            {
        //                // پیدا کردن foreign key در والد که به این child اشاره می‌کند
        //                var parentFkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, typeof(T));
        //                if (parentFkProp == null)
        //                    continue;

        //                var foreignKeyValue = parentFkProp.GetValue(mainObj);
        //                if (foreignKeyValue == null)
        //                    continue;

        //                // فراخوانی متد انتخاب با foreign key
        //                var method = typeof(DataSelectHelper)
        //                    .GetMethod(nameof(SelectByForeignKey))
        //                    .MakeGenericMethod(itemType);

        //                var listObj = method.Invoke(this, new object[] { foreignKeyValue });
        //                prop.SetValue(mainObj, listObj);
        //            }
        //            catch
        //            {
        //                // خطا نادیده گرفته می‌شود
        //            }
        //        }
        //    }

        //    return mainObj;
        //}

        public T SelectWithRelationsByPrimaryKey<T>(object keyValue) where T : new()
        {
            // ابتدا شی اصلی را انتخاب می‌کنیم
            var mainObj = SelectByPrimaryKey<T>(keyValue);
            if (mainObj == null) return default;

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // فقط فیلدهایی که [Ignore] دارند یعنی نمونه یا لیست کلاس دیگر
                if (!EntityMetadataHelper.IsIgnored(prop))
                    continue;

                Type propType = prop.PropertyType;

                // پیدا کردن foreign key در والد که به این child اشاره می‌کند
                var parentFkProp = EntityMetadataHelper.GetForeignKeyPropertyForParent(prop, typeof(T));
                if (parentFkProp == null)
                    continue;

                var foreignKeyValue = parentFkProp.GetValue(mainObj);
                if (foreignKeyValue == null)
                    continue;

                // نمونه کلاس دیگر (تک‌تایی)
                if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    try
                    {
                        var method = typeof(DataSelectHelper)
                            .GetMethod(nameof(SelectWithRelationsByPrimaryKey))
                            .MakeGenericMethod(propType);

                        var childObj = method.Invoke(this, new object[] { foreignKeyValue });

                        // set SystemInfoRef در child
                        if (childObj != null)
                        {
                            var fkPropInChild = EntityMetadataHelper.GetForeignKeyProperty(propType);
                            if (fkPropInChild != null)
                                fkPropInChild.SetValue(childObj, foreignKeyValue);
                        }

                        prop.SetValue(mainObj, childObj);
                    }
                    catch
                    {
                        // خطا نادیده گرفته می‌شود
                    }
                }
                // لیست از نمونه‌ها
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    try
                    {
                        var method = typeof(DataSelectHelper)
                            .GetMethod(nameof(SelectByForeignKey))
                            .MakeGenericMethod(itemType);

                        var listObj = method.Invoke(this, new object[] { foreignKeyValue });
                        if (listObj != null)
                        {
                            var enumerable = (IEnumerable)listObj;
                            foreach (var item in enumerable)
                            {
                                var fkPropInItem = EntityMetadataHelper.GetForeignKeyProperty(item.GetType());
                                if (fkPropInItem != null)
                                    fkPropInItem.SetValue(item, foreignKeyValue);
                            }
                            prop.SetValue(mainObj, listObj);
                        }
                    }
                    catch
                    {
                        // خطا نادیده گرفته می‌شود
                    }
                }
            }

            return mainObj;
        }


        #endregion

        #region توابع کمکی کاربردی

        public List<T> SelectByColumnValue<T>(string columnName, object value) where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val";
            var param = new SqlParameter("@val", value);
            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public List<T> SelectByCondition<T>(string whereClause, params SqlParameter[] parameters) where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT * FROM [{tableName}] WHERE {whereClause}";
            var dt = _dataHelper.ExecuteQuery(query, parameters);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public List<T> SelectTop<T>(int count, string orderByColumn = null) where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT TOP ({count}) * FROM [{tableName}]";
            if (!string.IsNullOrEmpty(orderByColumn))
                query += $" ORDER BY [{orderByColumn}]";

            var dt = _dataHelper.ExecuteQuery(query);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public bool Exists<T>(object keyValue) where T : new()
        {
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(typeof(T));
            var columnName = EntityMetadataHelper.GetColumnName(keyProp);
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT COUNT(1) FROM [{tableName}] WHERE [{columnName}] = @val";
            var param = new SqlParameter("@val", keyValue);
            var dt = _dataHelper.ExecuteQuery(query, param);
            return dt.Rows[0][0] != DBNull.Value && Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public int Count<T>() where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT COUNT(1) FROM [{tableName}]";
            var dt = _dataHelper.ExecuteQuery(query);
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        public int CountByCondition<T>(string whereClause, params SqlParameter[] parameters) where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT COUNT(1) FROM [{tableName}] WHERE {whereClause}";
            var dt = _dataHelper.ExecuteQuery(query, parameters);
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        #endregion
    }

}





