using System.Windows.Forms;

namespace Dh.Launcher.WinFormsTest
{
    public partial class TextViewerForm : Form
    {
        public TextViewerForm(){ InitializeComponent(); }
        public void LoadText(string title, string text){ this.Text = title; this.textBox1.Text = text ?? ""; }
    }
}
