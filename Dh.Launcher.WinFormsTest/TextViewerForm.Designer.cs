namespace Dh.Launcher.WinFormsTest
{
    partial class TextViewerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBox1;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(0, 0);
            this.textBox1.Multiline = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(780, 520);
            this.textBox1.TabIndex = 0;
            this.textBox1.WordWrap = false;
            // 
            // TextViewerForm
            // 
            this.ClientSize = new System.Drawing.Size(780, 520);
            this.Controls.Add(this.textBox1);
            this.Name = "TextViewerForm";
            this.Text = "Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
