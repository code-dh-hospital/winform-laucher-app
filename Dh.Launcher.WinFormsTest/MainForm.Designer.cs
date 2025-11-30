namespace Dh.Launcher.WinFormsTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnOpenDiag;
        private System.Windows.Forms.Label lblInfo;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.btnOpenDiag = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnOpenDiag
            // 
            this.btnOpenDiag.Location = new System.Drawing.Point(12, 46);
            this.btnOpenDiag.Name = "btnOpenDiag";
            this.btnOpenDiag.Size = new System.Drawing.Size(212, 29);
            this.btnOpenDiag.TabIndex = 0;
            this.btnOpenDiag.Text = "Open Diagnostics Explorer";
            this.btnOpenDiag.UseVisualStyleBackColor = true;
            this.btnOpenDiag.Click += new System.EventHandler(this.btnOpenDiag_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(12, 15);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(344, 13);
            this.lblInfo.TabIndex = 1;
            this.lblInfo.Text = "Launcher WinForms Test - Use Diagnostics to inspect manifests/logs.";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(520, 120);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnOpenDiag);
            this.Name = "MainForm";
            this.Text = "Launcher WinForms Test";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
