using System;
using System.Reflection;
using System.Windows.Forms;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Updater.DebugWinForms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version != null ? asm.GetName().Version.ToString() : "(no version)";
            var env = AppEnvironment.Initialize(typeof(MainForm).Namespace);
            this.Text = "Debug WinForms Core - v" + ver;
            this.labelVersion.Text = "Core version: " + ver;
            this.labelActive.Text = "Active version: " + (env.GetActiveVersion() ?? "(null)");
            this.labelInstalled.Text = "Installed: " + string.Join(", ", env.GetInstalledVersions());
            this.labelClientId.Text = "ClientId: " + (env.GetClientId() ?? "(null)");
        }
    }
}
