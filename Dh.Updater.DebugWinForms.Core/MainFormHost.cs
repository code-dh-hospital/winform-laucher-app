using System;
using System.Windows.Forms;

namespace Dh.Updater.DebugWinForms.Core
{
    public static class MainFormHost
    {
        [STAThread]
        public static int Run(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }
}
