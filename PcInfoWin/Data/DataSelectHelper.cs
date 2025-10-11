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
            // 1- واکشی شی اصلی
            var mainObj = SelectByPrimaryKey<T>(keyValue);
            if (mainObj == null)
                return default;

            // 2- بررسی propertyهای کلاس اصلی
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // فقط propertyهایی که [Ignore] دارند (زیرمجموعه)
                if (!EntityMetadataHelper.IsIgnored(prop))
                    continue;

                Type propType = prop.PropertyType;

                // تک‌شی
                if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    // مقدار SystemInfoRef را مستقیم به foreign key بده
                    try
                    {
                        var method = typeof(DataSelectHelper)
                            .GetMethod(nameof(SelectByForeignKey))
                            .MakeGenericMethod(propType);

                        // فراخوانی SelectByForeignKey با SystemInfoID
                        var childObjList = (IList)method.Invoke(this, new object[] { keyValue });
                        // چون تک‌شی است، فقط اولین مورد را ست می‌کنیم
                        prop.SetValue(mainObj, childObjList.Cast<object>().FirstOrDefault());
                    }
                    catch
                    {
                        continue;
                    }
                }
                // لیست
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    try
                    {
                        var method = typeof(DataSelectHelper)
                            .GetMethod(nameof(SelectByForeignKey))
                            .MakeGenericMethod(itemType);

                        var listObj = method.Invoke(this, new object[] { keyValue });
                        prop.SetValue(mainObj, listObj);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return mainObj;
        }



        public SystemInfo SelectSystemInfoWithRelations(int systemInfoId)
        {
            // 1- خواندن شی اصلی
            SystemInfo mainObj = SelectByPrimaryKey<SystemInfo>(systemInfoId);
            if (mainObj == null) return null;

            // 2- گرفتن تمام propertyهای [Ignore] (یعنی کلاس یا لیست زیرمجموعه)
            var props = typeof(SystemInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => Attribute.IsDefined(p, typeof(IgnoreAttribute)));

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                // اگر تک‌شی است (Class)
                if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var method = typeof(DataSelectHelper)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(propType);

                    // اجرای کوئری با شرط SystemInfoRef = systemInfoId
                    var childObj = method.Invoke(this, new object[] { systemInfoId });
                    // چون SelectByForeignKey همیشه لیست برمی‌گرداند، اولین مورد را انتخاب می‌کنیم
                    var firstItem = ((IEnumerable)childObj).Cast<object>().FirstOrDefault();
                    prop.SetValue(mainObj, firstItem);
                }
                // اگر لیست است
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    var method = typeof(DataSelectHelper)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(itemType);

                    var listObj = method.Invoke(this, new object[] { systemInfoId });
                    prop.SetValue(mainObj, listObj);
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





