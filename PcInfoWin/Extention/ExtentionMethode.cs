using PcInfoWin.Properties;
using SqlDataExtention.Entity.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcInfoWin.Extention
{
    public static class ExtentionMethode
    {
        public static void updateCurreentInfoFromUserInput(SystemInfo curreentInfo)
        {
            curreentInfo.pcCodeInfo[0].SystemInfoRef = PcCodeForm._pcCodeInfo.SystemInfoRef;
            curreentInfo.pcCodeInfo[0].PcCode = PcCodeForm._pcCodeInfo.PcCode;
            curreentInfo.pcCodeInfo[0].UserFullName = PcCodeForm._pcCodeInfo.UserFullName;
            curreentInfo.pcCodeInfo[0].PersonnelCode = PcCodeForm._pcCodeInfo.PersonnelCode;
            curreentInfo.pcCodeInfo[0].Unit = PcCodeForm._pcCodeInfo.Unit;
            curreentInfo.pcCodeInfo[0].Desc1 = PcCodeForm._pcCodeInfo.Desc1;
            curreentInfo.pcCodeInfo[0].Desc2 = PcCodeForm._pcCodeInfo.Desc2;
            curreentInfo.pcCodeInfo[0].Desc3 = PcCodeForm._pcCodeInfo.Desc3;
        }

        public static void updateCurreentInfoFromDBInfo(SystemInfo curreentInfo, SystemInfo infoFromDB)
        {
            curreentInfo.SystemInfoID = infoFromDB.SystemInfoID;
            curreentInfo.pcCodeInfo[0].PcCodeInfoID = infoFromDB.pcCodeInfo[0].PcCodeInfoID;
            curreentInfo.pcCodeInfo[0].SystemInfoRef = infoFromDB.pcCodeInfo[0].SystemInfoRef;
            curreentInfo.pcCodeInfo[0].PcCode = infoFromDB.pcCodeInfo[0].PcCode;
            curreentInfo.pcCodeInfo[0].PersonnelCode = infoFromDB.pcCodeInfo[0].PersonnelCode;
            curreentInfo.pcCodeInfo[0].UserFullName = infoFromDB.pcCodeInfo[0].UserFullName;
            curreentInfo.pcCodeInfo[0].Unit = infoFromDB.pcCodeInfo[0].Unit;
            curreentInfo.pcCodeInfo[0].Desc1 = infoFromDB.pcCodeInfo[0].Desc1;
            curreentInfo.pcCodeInfo[0].Desc2 = infoFromDB.pcCodeInfo[0].Desc2;
            curreentInfo.pcCodeInfo[0].Desc3 = infoFromDB.pcCodeInfo[0].Desc3;
            curreentInfo.pcCodeInfo[0].InsertDate = infoFromDB.pcCodeInfo[0].InsertDate;
            curreentInfo.updateInfo.UpdatePath = infoFromDB.updateInfo.UpdatePath;

        }


        public static void updateSettingsDefaultFromCurreentInfo(SystemInfo curreentInfo)
        {
            Settings.Default.SystemInfoID = curreentInfo.pcCodeInfo[0].SystemInfoRef;
            Settings.Default.PcCode = curreentInfo.pcCodeInfo[0].PcCode;
            Settings.Default.IpAddress = curreentInfo.NetworkAdapterInfo[0].IpAddress;
            Settings.Default.MacAddress = curreentInfo.NetworkAdapterInfo[0].MACAddress;
            Settings.Default.Desc1 = curreentInfo.pcCodeInfo[0].Desc1;
            Settings.Default.PathUpdate = curreentInfo.updateInfo.UpdatePath;
            Settings.Default.Save();
        }
        public static void updateBalonInfoFromSettings()
        {
            TrayApplication.PcCode = Settings.Default.PcCode;
            TrayApplication.IpAddress = Settings.Default.IpAddress;
            TrayApplication.MacAddress = Settings.Default.MacAddress;
            TrayApplication.Desc1 = Settings.Default.Desc1;
        }
    }
}
