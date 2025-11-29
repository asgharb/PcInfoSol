using SqlDataExtention.Attributes;
using SqlDataExtention.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace SqlDataExtention.Data
{
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
            string query = $"SELECT * FROM [{tableName}] WHERE ([ExpireDate] IS NULL OR [ExpireDate] = '')";
            var dt = _dataHelper.ExecuteQuery(query);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public List<T> SelectAllWitoutConditonal<T>() where T : new()
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

            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val AND ([ExpireDate] IS NULL OR [ExpireDate] = '')";
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

            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val AND ([ExpireDate] IS NULL OR [ExpireDate] = '')";
            var param = new SqlParameter("@val", foreignValue);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt);
        }

        #endregion

        #region واکشی بازگشتی با فیلتر Generic و type-safe

        public T SelectWithRelationsByPrimaryKey_Filter<T>(object keyValue, Func<T, bool> filter)
            where T : class, new()
        {
            var mainObj = SelectByPrimaryKey<T>(keyValue);
            if (mainObj == null || !filter(mainObj)) return default;

            FillRelations_Filter(mainObj, filter);
            return mainObj;
        }

        public List<T> SelectAllWithRelations_Filter<T>(Func<T, bool> filter)
            where T : class, new()
        {
            var list = SelectAll<T>().Where(filter).ToList();
            foreach (var item in list)
                FillRelations_Filter(item, filter);
            return list;
        }
        private void FillRelations_Filter<T>(T mainObj, Func<T, bool> filter) where T : class
        {
            if (mainObj == null) return;

            Type type = mainObj.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => EntityMetadataHelper.IsIgnored(p));

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;
                var keyValue = EntityMetadataHelper.GetPrimaryKeyProperty(type).GetValue(mainObj);

                // تک‌شی (کلاس)
                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var method = typeof(DataSelectHelper)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(propType);

                    var listObj = method.Invoke(this, new object[] { keyValue }) as IEnumerable;
                    var firstItem = listObj?.Cast<object>().FirstOrDefault();

                    prop.SetValue(mainObj, firstItem);

                    if (firstItem != null)
                        CallFillRelationsGenericDynamic(firstItem); // بازگشت به پر کردن فرزندان
                }
                // لیست
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    var method = typeof(DataSelectHelper)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(itemType);

                    var listObj = method.Invoke(this, new object[] { keyValue }) as IEnumerable;

                    // ایجاد List<itemType> صحیح
                    var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                    if (listObj != null)
                    {
                        foreach (var item in listObj)
                        {
                            typedList.Add(item);
                            CallFillRelationsGenericDynamic(item); // بازگشت برای فرزندان
                        }
                    }

                    prop.SetValue(mainObj, typedList);
                }
            }
        }

        public List<T> SelectByColumn<T>(string columnName, object value) where T : new()
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName));

            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);

            // بررسی وجود ستون در کلاس
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .FirstOrDefault(p => EntityMetadataHelper.GetColumnName(p).Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (prop == null)
                throw new ArgumentException($"Column '{columnName}' not found in entity {type.Name}.");

            string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @val AND ([ExpireDate] IS NULL OR [ExpireDate] = '')";
            var param = new SqlParameter("@val", value ?? DBNull.Value);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt);
        }

        // نسخه بدون filter برای فرزندان
        private void CallFillRelationsGenericDynamic(object obj)
        {
            if (obj == null) return;
            var method = typeof(DataSelectHelper)
                .GetMethod(nameof(FillRelations_Filter), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(obj.GetType());
            method.Invoke(this, new object[] { obj, null }); // filter = null برای فرزندان
        }

        #endregion

        #region واکشی بازگشتی بدون فیلتر (اورلود)

        /// <summary>
        /// واکشی یک شی به همراه تمام زیرمجموعه‌ها بدون شرط
        /// </summary>
        public T SelectWithRelationsByPrimaryKey<T>(object keyValue)
            where T : class, new()
        {
            return SelectWithRelationsByPrimaryKey_Filter<T>(keyValue, x => true);
        }

        /// <summary>
        /// واکشی همه اشیا به همراه روابط بدون شرط
        /// </summary>
        public List<T> SelectAllWithRelations<T>()
            where T : class, new()
        {
            return SelectAllWithRelations_Filter<T>(x => true);
        }
        #endregion

        public List<string> GetAllPcCodes()
        {
            string query = "SELECT PcCode FROM PcCodeInfo";
            var dt = _dataHelper.ExecuteQuery(query);
            var pcCodes = new List<string>();
            foreach (System.Data.DataRow row in dt.Rows)
            {
                pcCodes.Add(row["PcCode"].ToString());
            }
            return pcCodes;
        }




    }
}
