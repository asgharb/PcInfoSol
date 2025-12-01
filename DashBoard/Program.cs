using System;
using System.Windows.Forms;
namespace DashBoard
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. ابتدا فرم لاگین را بسازید
            FrmLogin loginForm = new FrmLogin();

            // 2. آن را به صورت دیالوگ نمایش دهید و چک کنید آیا OK برگردانده یا خیر
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                // 3. اگر لاگین موفق بود، حالا فرم اصلی را اجرا کنید
                // این باعث می‌شود FrmMain تبدیل به فرم اصلی برنامه شود
                Application.Run(new FrmMain());
            }
            else
            {
                // اگر کاربر لاگین را بست یا کنسل کرد، برنامه تمام می‌شود
                Application.Exit();
            }
        }

    }
}
