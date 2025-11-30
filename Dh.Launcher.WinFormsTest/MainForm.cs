using System;
using System.Windows.Forms;

namespace Dh.Launcher.WinFormsTest
{
    public partial class MainForm : Form
    {
        public MainForm(){ InitializeComponent(); }
        private void btnOpenDiag_Click(object sender, EventArgs e){ using (var f = new DiagnosticsExplorerForm()){ f.ShowDialog(this); } }
    }
}
