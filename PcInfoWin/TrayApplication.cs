using PcInfoWin.Properties;
using SqlDataExtention.Data;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace PcInfoWin
{
    public class TrayApplication : IDisposable
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;

        public static string PcCode = string.Empty;
        public static string IpAddress = string.Empty;
        public static string MacAddress = string.Empty;
        public static string Desc1 = string.Empty;

        public TrayApplication()
        {
            trayMenu = BuildMenu();
            trayIcon = BuildTrayIcon();
            trayIcon.Visible = true;
        }

        private NotifyIcon BuildTrayIcon()
        {
            var icon = new NotifyIcon
            {
                Icon = Properties.Resources.pc,
                Text = "PcInfo V:" + Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                ContextMenuStrip = trayMenu
            };

            icon.MouseClick += OnTrayIconMouseClick;
            return icon;
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("تغییر PC_Code", null, (s, e) => ShowPcCodeForm(true));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("خروج", null, (s, e) => ShowPcCodeForm(false));
            return menu;
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                trayIcon.BalloonTipText = "PC_Code: " + PcCode;
                trayIcon.BalloonTipText += "\nMac: " + MacAddress;
                trayIcon.BalloonTipText += "\nIP: " + IpAddress;
                trayIcon.BalloonTipText += "\nDescription: " + Desc1;
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3000);
            }
        }

        private void ShowPcCodeForm(bool editMode)
        {
            try
            {
                PcCodeForm.IsEditMode = editMode;
                using (var form = new PcCodeForm())
                {
                    form.ShowDialog();
                }
                if (PcCodeForm.IsEditMode && !PcCodeForm.IsNewMode && PcCodeForm.resultImportData)
                {
                    var helper = new DataInsertUpdateHelper();

                    int systemInfoRef = Settings.Default.SystemInfoID;

                    bool result = helper.ExpireAndInsertPcCodeInfo(systemInfoRef, PcCodeForm._pcCodeInfo);
                    if (result)
                    {
                        MessageBox.Show("عملیات با موفقیت انجام شد.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("عملیات با خطا مواجه شد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "---", SysId: Settings.Default.SystemInfoID > 0 ? Settings.Default.SystemInfoID : 0);
            }
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
            trayMenu?.Dispose();
        }
    }
}