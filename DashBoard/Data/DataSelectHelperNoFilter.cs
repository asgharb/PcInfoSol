using DashBoard.Data;
using SqlDataExtention.Data;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;


//var helper = new DataSelectHelperNoFilter();
//var allSystems = helper.SelectAllFullSystemInfo(); // تمام SystemInfo ها
//var singleSystem = helper.SelectFullSystemInfo(5); // یک SystemInfo به همراه همه روابط

namespace DashBoard.Data
{
    public class DataSelectHelperNoFilter
    {
        private readonly DataHelper _dataHelper;

        public DataSelectHelperNoFilter()
        {
            _dataHelper = new DataHelper();
        }

        #region پایه: واکشی ساده بدون فیلتر

        public List<T> SelectAll<T>() where T : new()
        {
            string tableName = EntityMetadataHelper.GetTableName(typeof(T));
            string query = $"SELECT * FROM [{tableName}] ORDER BY {GetPrimaryKeyColumn(typeof(T))}";
            var dt = _dataHelper.ExecuteQuery(query);
            return _dataHelper.ConvertToList<T>(dt);
        }

        public T SelectByPrimaryKey<T>(object keyValue) where T : new()
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            string keyColumn = GetPrimaryKeyColumn(type);

            string query = $"SELECT * FROM [{tableName}] WHERE [{keyColumn}] = @val";
            var param = new SqlParameter("@val", keyValue);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt).FirstOrDefault();
        }

        public List<T> SelectByForeignKey<T>(object foreignValue) where T : new()
        {
            Type type = typeof(T);
            string tableName = EntityMetadataHelper.GetTableName(type);
            string fkColumn = GetForeignKeyColumn(type);

            string query = $"SELECT * FROM [{tableName}] WHERE [{fkColumn}] = @val ORDER BY {GetPrimaryKeyColumn(type)}";
            var param = new SqlParameter("@val", foreignValue);

            var dt = _dataHelper.ExecuteQuery(query, param);
            return _dataHelper.ConvertToList<T>(dt);
        }

        #endregion

        #region واکشی بازگشتی کامل بدون فیلتر

        public SystemInfo SelectFullSystemInfo(int systemInfoID)
        {
            var main = SelectByPrimaryKey<SystemInfo>(systemInfoID);
            if (main == null) return null;

            FillRelationsRecursive(main);
            return main;
        }

        public List<SystemInfo> SelectAllFullSystemInfo()
        {
            var list = SelectAll<SystemInfo>();
            foreach (var item in list)
            {
                FillRelationsRecursive(item);
            }
            return list;
        }

        private void FillRelationsRecursive<T>(T mainObj) where T : class
        {
            if (mainObj == null) return;

            Type type = mainObj.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => EntityMetadataHelper.IsIgnored(p));

            var primaryKeyValue = EntityMetadataHelper.GetPrimaryKeyProperty(type).GetValue(mainObj);

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                // تک‌شی (کلاس)
                if (!typeof(IEnumerable).IsAssignableFrom(propType) || propType == typeof(string))
                {
                    var method = typeof(DataSelectHelperNoFilter)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(propType);

                    var listObj = method.Invoke(this, new object[] { primaryKeyValue }) as IEnumerable;
                    var firstItem = listObj?.Cast<object>().FirstOrDefault();
                    prop.SetValue(mainObj, firstItem);

                    if (firstItem != null)
                        FillRelationsRecursive(firstItem);
                }
                // لیست
                else if (propType.IsGenericType)
                {
                    Type itemType = propType.GetGenericArguments()[0];
                    var method = typeof(DataSelectHelperNoFilter)
                        .GetMethod(nameof(SelectByForeignKey))
                        .MakeGenericMethod(itemType);

                    var listObj = method.Invoke(this, new object[] { primaryKeyValue }) as IEnumerable;

                    var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                    if (listObj != null)
                    {
                        foreach (var item in listObj.Cast<object>()
                                                     .OrderBy(x => EntityMetadataHelper.GetPrimaryKeyProperty(x.GetType()).GetValue(x)))
                        {
                            typedList.Add(item);
                            FillRelationsRecursive(item);
                        }

                    }
                    prop.SetValue(mainObj, typedList);
                }
            }
        }

        #endregion

        #region متدهای کمکی

        private string GetPrimaryKeyColumn(Type type)
        {
            var keyProp = EntityMetadataHelper.GetPrimaryKeyProperty(type);
            return EntityMetadataHelper.GetColumnName(keyProp);
        }

        private string GetForeignKeyColumn(Type type)
        {
            var fkProp = EntityMetadataHelper.GetForeignKeyProperty(type);
            return EntityMetadataHelper.GetColumnName(fkProp);
        }

        #endregion
    }
}
