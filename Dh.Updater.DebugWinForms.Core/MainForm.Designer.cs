namespace Dh.Updater.DebugWinForms.Core
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelActive;
        private System.Windows.Forms.Label labelInstalled;
        private System.Windows.Forms.Label labelClientId;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelActive = new System.Windows.Forms.Label();
            this.labelInstalled = new System.Windows.Forms.Label();
            this.labelClientId = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true; this.labelVersion.Location = new System.Drawing.Point(12, 15);
            this.labelVersion.Name = "labelVersion"; this.labelVersion.Size = new System.Drawing.Size(73, 13); this.labelVersion.TabIndex = 0;
            this.labelVersion.Text = "Core version: ";
            // 
            // labelActive
            // 
            this.labelActive.AutoSize = true; this.labelActive.Location = new System.Drawing.Point(12, 40);
            this.labelActive.Name = "labelActive"; this.labelActive.Size = new System.Drawing.Size(83, 13); this.labelActive.TabIndex = 1;
            this.labelActive.Text = "Active version: ";
            // 
            // labelInstalled
            // 
            this.labelInstalled.AutoSize = true; this.labelInstalled.Location = new System.Drawing.Point(12, 65);
            this.labelInstalled.Name = "labelInstalled"; this.labelInstalled.Size = new System.Drawing.Size(53, 13); this.labelInstalled.TabIndex = 2;
            this.labelInstalled.Text = "Installed: ";
            // 
            // labelClientId
            // 
            this.labelClientId.AutoSize = true; this.labelClientId.Location = new System.Drawing.Point(12, 90);
            this.labelClientId.Name = "labelClientId"; this.labelClientId.Size = new System.Drawing.Size(54, 13); this.labelClientId.TabIndex = 3;
            this.labelClientId.Text = "ClientId: ";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(560, 130);
            this.Controls.Add(this.labelClientId); this.Controls.Add(this.labelInstalled); this.Controls.Add(this.labelActive); this.Controls.Add(this.labelVersion);
            this.Name = "MainForm"; this.Text = "Debug WinForms Core"; this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
