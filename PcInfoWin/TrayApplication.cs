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
                trayIcon.BalloonTipTitle = "اطلاعات سیستم";
                trayIcon.BalloonTipText = "PC_Code: 12345\nوضعیت: فعال";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3000);
            }
        }

        private void ShowPcCodeForm(bool isEditMode)
        {
            using (var form = new PcCodeForm())
            {
                form.IsEditMode = isEditMode;
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