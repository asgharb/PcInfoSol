using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using SqlDataExtention.Attributes;

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

        // نسخه بدون filter برای فرزندان
        private void CallFillRelationsGenericDynamic(object obj)
        {
            if (obj == null) return;
            var method = typeof(DataSelectHelper)
                .GetMethod(nameof(FillRelations_Filter), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(obj.GetType());
            method.Invoke(this, new object[] { obj, null }); // filter = null برای فرزندان
        }

        private bool IsOfTypeAndPassFilter<TFilter>(object obj, Func<TFilter, bool> filter)
        {
            if (filter == null) return true;
            if (obj is TFilter tObj)
                return filter(tObj);
            return false;
        }

        private void CallFillRelationsGeneric<TCaller>(object obj, Func<TCaller, bool> filter) where TCaller : class
        {
            // اگر obj از نوع TCaller است، فراخوانی کنیم
            if (obj is TCaller tObj)
                FillRelations_Filter(tObj, filter);
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

        #region --- Helpers ---

        /// <summary>
        /// ایجاد یک List<itemType> از IEnumerable<object> به‌صورت بازتابی
        /// (از Enumerable.Cast<itemType>().ToList() استفاده می‌کند)
        /// </summary>
        private object CreateGenericListFromObjects(IEnumerable<object> objects, Type itemType)
        {
            if (objects == null) return null;

            // Enumerable.Cast<itemType>(objects)
            var castMethod = typeof(Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(itemType);

            // Enumerable.ToList<itemType>(IEnumerable<itemType>)
            var toListMethod = typeof(Enumerable).GetMethod("ToList", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(itemType);

            var casted = castMethod.Invoke(null, new object[] { objects });
            var list = toListMethod.Invoke(null, new object[] { casted });
            return list;
        }

        #endregion
    }
}
