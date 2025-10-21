using PcInfoWin.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcInfoWin
{
    public class TrayApplication : IDisposable
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;

        public static string IpAddress = string.Empty;
        public static string MacAddress = string.Empty;

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
                Icon = SystemIcons.Information, // آیکون ویندوز
                Text = "برنامه من",
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
                trayIcon.BalloonTipText = "PC_Code: " + Settings.Default.PcCode;
                trayIcon.BalloonTipText += "\nMac: " + MacAddress;
                trayIcon.BalloonTipText += "\nIP: " + IpAddress;
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3000);
            }
        }

        private void ShowPcCodeForm(bool isEditMode)
        {
            PcCodeForm.IsEditMode = isEditMode;
            using (var form = new PcCodeForm())
            {
                form.ShowDialog();
            }
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
            trayMenu?.Dispose();
        }
    }
}