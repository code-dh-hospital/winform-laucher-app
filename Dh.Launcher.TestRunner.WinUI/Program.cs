using System;
using System.Windows.Forms;

namespace Dh.Launcher.TestRunner.WinUI
{
    internal static class Program
    {
        /// <summary>
        /// Entry point WinForms mini UI cho TestRunner.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
