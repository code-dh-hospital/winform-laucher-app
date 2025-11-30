using System;
using System.Windows.Forms;
using Dh.AppLauncher.Core;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Launcher.WinFormsTest
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Đảm bảo chỉ một instance sample WinForms launcher chạy.
            using (var guard = new LauncherInstanceGuard("Dh.Updater.SampleApp"))
            {
                if (!guard.HasHandle)
                {
                    MessageBox.Show("Another instance of Dh.Updater.SampleApp is already running.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var env = AppEnvironment.Initialize("Dh.Updater.SampleApp");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
