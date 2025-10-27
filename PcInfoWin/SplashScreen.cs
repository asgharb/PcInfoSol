using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcInfoWin
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {

            InitializeComponent();
            closeSplashScreen();
        }
        private async void closeSplashScreen()
        {
            await Task.Delay(1500);
            this.Close();
        }
        private void SplashScreen_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            this.Opacity = 0.5;
        }
    }
}

