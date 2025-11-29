using SqlDataExtention.Attributes;
using SqlDataExtention.Data;
using SqlDataExtention.Entity;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;


namespace DashBoard.Data
{
    public class DataSelectHelperNoFilter
    {
        private readonly DataHelper _dataHelper;
        private readonly DataSelectHelper _dataSelectHelper = new DataSelectHelper();

        public DataSelectHelperNoFilter()
        {
            _dataHelper = new DataHelper();
        }

        #region پایه: واکشی ساده بدون فیلتر

        //public List<T> SelectAll<T>() where T : new()
        //{
        //    string tableName = EntityMetadataHelper.GetTableName(typeof(T));
        //    string query = $"SELECT * FROM [{tableName}] ORDER BY {GetPrimaryKeyColumn(typeof(T))}";
        //    var dt = _dataHelper.ExecuteQuery(query);
        //    return _dataHelper.ConvertToList<T>(dt);
        //}

        //public T SelectByPrimaryKey<T>(object keyValue) where T : new()
        //{
        //    Type type = typeof(T);
        //    string tableName = EntityMetadataHelper.GetTableName(type);
        //    string keyColumn = GetPrimaryKeyColumn(type);

        //    string query = $"SELECT * FROM [{tableName}] WHERE [{keyColumn}] = @val";
        //    var param = new SqlParameter("@val", keyValue);

        //    var dt = _dataHelper.ExecuteQuery(query, param);
        //    return _dataHelper.ConvertToList<T>(dt).FirstOrDefault();
        //}

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

        //public SystemInfo SelectFullSystemInfo(int systemInfoID)
        //{
        //    var main = SelectByPrimaryKey<SystemInfo>(systemInfoID);
        //    if (main == null) return null;

        //    FillRelationsRecursive(main);
        //    return main;
        //}

        //public List<SystemInfo> SelectAllFullSystemInfo()
        //{
        //    var list = SelectAll<SystemInfo>();
        //    foreach (var item in list)
        //    {
        //        FillRelationsRecursive(item);
        //    }
        //    return list;
        //}

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


        public List<SystemInfo> SelectAllFullSystemInfo()
        {
            var cache = PreloadedDataCache.LoadAll(_dataSelectHelper);
            DataLinker.LinkAllRelations(cache);
            return cache.SystemInfos;
        }

        //public SystemInfo SelectFullSystemInfo(int id)
        //{
        //    var cache = PreloadedDataCache.LoadAll(_dataSelectHelper);
        //    DataLinker.LinkAllRelations(cache);
        //    return cache.SystemInfoById[id];
        //}


    }



    public class PreloadedDataCache
    {
        public List<SystemInfo> SystemInfos { get; set; }
        public Dictionary<int, SystemInfo> SystemInfoById { get; set; }

        public List<CpuInfo> CpuInfos { get; set; }
        public List<GpuInfo> GpuInfos { get; set; }
        public List<MotherboardInfo> MotherboardInfos { get; set; }
        public List<NetworkAdapterInfo> NetworkAdapterInfos { get; set; }
        public List<RamModuleInfo> RamModuleInfos { get; set; }
        public List<DiskInfo> DiskInfos { get; set; }
        public List<SystemEnvironmentInfo> SystemEnvironmentInfos { get; set; }
        public List<PcCodeInfo> PcCodeInfos { get; set; }
        public List<OpticalDriveInfo> OpticalDriveInfos { get; set; }
        public List<MonitorInfo> MonitorInfos { get; set; }
        public List<RamSummaryInfo> RamSummaryInfos { get; set; }
        public List<UpdateInfo> UpdateInfos { get; set; }
        public List<SwithInfo> SwithInfos { get; set; }

        public static PreloadedDataCache LoadAll(DataSelectHelper db)
        {
            var cache = new PreloadedDataCache();

            cache.SystemInfos = db.SelectAll<SystemInfo>();
            cache.SystemInfoById = cache.SystemInfos.ToDictionary(x => x.SystemInfoID);

            cache.CpuInfos = db.SelectAll<CpuInfo>();
            cache.GpuInfos = db.SelectAll<GpuInfo>();
            cache.MotherboardInfos = db.SelectAll<MotherboardInfo>();
            cache.NetworkAdapterInfos = db.SelectAll<NetworkAdapterInfo>();
            cache.RamModuleInfos = db.SelectAll<RamModuleInfo>();
            cache.DiskInfos = db.SelectAll<DiskInfo>();
            cache.SystemEnvironmentInfos = db.SelectAll<SystemEnvironmentInfo>();
            cache.PcCodeInfos = db.SelectAll<PcCodeInfo>();
            cache.OpticalDriveInfos = db.SelectAll<OpticalDriveInfo>();
            cache.MonitorInfos = db.SelectAll<MonitorInfo>();
            cache.RamSummaryInfos = db.SelectAll<RamSummaryInfo>();
            cache.UpdateInfos = db.SelectAll<UpdateInfo>();
            cache.SwithInfos = db.SelectAll<SwithInfo>();

            return cache;
        }
    }

    public static class DataLinker
    {
        public static void LinkAllRelations(PreloadedDataCache cache)
        {
            foreach (var si in cache.SystemInfos)
            {
                int id = si.SystemInfoID;

                si.cpuInfo = cache.CpuInfos.FirstOrDefault(x => x.SystemInfoRef == id);
                si.gpuInfo = cache.GpuInfos.FirstOrDefault(x => x.SystemInfoRef == id);
                si.motherboardInfo = cache.MotherboardInfos.FirstOrDefault(x => x.SystemInfoRef == id);
                si.RamSummaryInfo = cache.RamSummaryInfos.FirstOrDefault(x => x.SystemInfoRef == id);
                si.updateInfo = cache.UpdateInfos.FirstOrDefault(x => x.SystemInfoRef == id);
                si.SwithInfo = cache.SwithInfos.FirstOrDefault(x => x.SystemInfoRef == id);

                si.systemEnvironmentInfo = cache.SystemEnvironmentInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.pcCodeInfo = cache.PcCodeInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.DiskInfo = cache.DiskInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.NetworkAdapterInfo = cache.NetworkAdapterInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.RamModuleInfo = cache.RamModuleInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.OpticalDriveInfo = cache.OpticalDriveInfos
                    .Where(x => x.SystemInfoRef == id).ToList();

                si.monitorInfo = cache.MonitorInfos
                    .Where(x => x.SystemInfoRef == id).ToList();
            }
        }
    }

















    //public class PreloadedDataCache
    //{
    //    public List<SystemInfo> SystemInfos { get; set; }
    //    public Dictionary<int, SystemInfo> SystemInfoById { get; set; }

    //    public List<SystemEnvironmentInfo> systemEnvironmentInfo { get; set; }

    //    public List<PcCodeInfo> pcCodeInfo { get; set; }

    //    public List<CpuInfo> cpuInfo { get; set; }

    //    public List<GpuInfo> gpuInfo { get; set; } 

    //    public List<MotherboardInfo> motherboardInfo { get; set; }

    //    public List<RamSummaryInfo> RamSummaryInfo { get; set; }

    //    public List<DiskInfo> DiskInfo { get; set; }

    //    public List<NetworkAdapterInfo> NetworkAdapterInfo { get; set; }

    //    public List<RamModuleInfo> RamModuleInfo { get; set; }

    //    public List<OpticalDriveInfo> OpticalDriveInfo { get; set; }

    //    public List<MonitorInfo> monitorInfo { get; set; }

    //    public static PreloadedDataCache LoadAll(DataSelectHelper db)
    //    {
    //        return new PreloadedDataCache
    //        {
    //            SystemInfos = db.SelectAll<SystemInfo>(),
    //            SystemInfoById = db.SelectAll<SystemInfo>().ToDictionary(x => x.SystemInfoID),

    //            systemEnvironmentInfo = db.SelectAll<SystemEnvironmentInfo>(),
    //            pcCodeInfo=db.SelectAll<PcCodeInfo>(),
    //            cpuInfo = db.SelectAll<CpuInfo>(),
    //            gpuInfo = db.SelectAll<GpuInfo>(),
    //            motherboardInfo = db.SelectAll<MotherboardInfo>(),
    //            RamSummaryInfo = db.SelectAll<RamSummaryInfo>(),
    //            DiskInfo = db.SelectAll<DiskInfo>(),
    //            NetworkAdapterInfo = db.SelectAll<NetworkAdapterInfo>(),
    //            RamModuleInfo = db.SelectAll<RamModuleInfo>(),
    //            OpticalDriveInfo= db.SelectAll<OpticalDriveInfo>(),
    //            monitorInfo= db.SelectAll<MonitorInfo>()
    //        };
    //    }
    //}


    //public static class DataLinker
    //{
    //    public static void LinkAllRelations(PreloadedDataCache cache)
    //    {
    //        // تک به یک
    //        foreach (var cpu in cache.cpuInfo)
    //            cpu.SystemInfo = cache.SystemInfoById[cpu.SystemInfoRef];

    //        foreach (var gpu in cache.gpuInfo)
    //            gpu.SystemInfo = cache.SystemInfoById[gpu.SystemInfoRef];

    //        foreach (var mb in cache.motherboardInfo)
    //            mb.SystemInfo = cache.SystemInfoById[mb.SystemInfoRef];

    //        // یک به چند
    //        foreach (var si in cache.SystemInfos)
    //        {
    //            si.cpuInfo = cache.CpuInfos.FirstOrDefault(x => x.SystemInfoRef == si.SystemInfoID);
    //            si.gpuInfo = cache.GpuInfos.FirstOrDefault(x => x.SystemInfoRef == si.SystemInfoID);
    //            si.motherboardInfo = cache.MotherboardInfos.FirstOrDefault(x => x.SystemInfoRef == si.SystemInfoID);

    //            si.networkAdapterInfo = cache.NetworkAdapterInfos.Where(x => x.SystemInfoRef == si.SystemInfoID).ToList();
    //            si.ramModuleInfo = cache.RamModuleInfos.Where(x => x.SystemInfoRef == si.SystemInfoID).ToList();
    //            si.diskInfo = cache.DiskInfos.Where(x => x.SystemInfoRef == si.SystemInfoID).ToList();
    //            si.systemEnvironmentInfo = cache.SystemEnvironmentInfos.Where(x => x.SystemInfoRef == si.SystemInfoID).ToList();
    //        }
    //    }
    //}


}
