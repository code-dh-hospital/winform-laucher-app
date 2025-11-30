using System;
using System.Windows.Forms;
using Dh.AppLauncher;
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

                var paths = Dh.Updater.DebugWinForms.Core.AppPathsDebugger.DumpPaths(env);
                Console.WriteLine("[WF PATHS] BinaryRoot=" + paths.BinaryRoot);
                Console.WriteLine("[WF PATHS] DataRoot=" + paths.DataRoot);
                Console.WriteLine("[WF PATHS] SharedDataRoot=" + paths.SharedDataRoot);
                Console.WriteLine("[WF PATHS] CurrentDirectory=" + Environment.CurrentDirectory);
                try
                {
                    System.IO.File.WriteAllText("winforms_relative_test.txt",
                        "Hello from WinForms relative path at " + DateTime.Now.ToString("O"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WF PATHS] error write: " + ex.Message);
                }
                Application.Run(new MainForm());
            }
        }
    }
}
