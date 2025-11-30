namespace Dh.Launcher.WinFormsTest
{
    partial class DiagnosticsExplorerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeFolders;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabExplorer;
        private System.Windows.Forms.ListView listFiles;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.ColumnHeader colMd5;
        private System.Windows.Forms.ColumnHeader colModified;
        private System.Windows.Forms.TabPage tabManifests;
        private System.Windows.Forms.Button btnViewChangelogs;
        private System.Windows.Forms.ListView listManifests;
        private System.Windows.Forms.ColumnHeader colVer;
        private System.Windows.Forms.ColumnHeader colFileCount;
        private System.Windows.Forms.ColumnHeader colHasChangelog;
        private System.Windows.Forms.TabPage tabLogs;
        private System.Windows.Forms.Button btnCreateSnapshot;
        private System.Windows.Forms.Button btnOpenLog;
        private System.Windows.Forms.ListBox listLogs;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeFolders = new System.Windows.Forms.TreeView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabExplorer = new System.Windows.Forms.TabPage();
            this.listFiles = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colMd5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colModified = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabManifests = new System.Windows.Forms.TabPage();
            this.btnViewChangelogs = new System.Windows.Forms.Button();
            this.listManifests = new System.Windows.Forms.ListView();
            this.colVer = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colFileCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHasChangelog = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabLogs = new System.Windows.Forms.TabPage();
            this.btnCreateSnapshot = new System.Windows.Forms.Button();
            this.btnOpenLog = new System.Windows.Forms.Button();
            this.listLogs = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabExplorer.SuspendLayout();
            this.tabManifests.SuspendLayout();
            this.tabLogs.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeFolders);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(884, 511);
            this.splitContainer1.SplitterDistance = 250;
            this.splitContainer1.TabIndex = 0;
            // 
            // treeFolders
            // 
            this.treeFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeFolders.Location = new System.Drawing.Point(0, 0);
            this.treeFolders.Name = "treeFolders";
            this.treeFolders.Size = new System.Drawing.Size(250, 511);
            this.treeFolders.TabIndex = 0;
            this.treeFolders.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeFolders_AfterSelect);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabExplorer);
            this.tabControl1.Controls.Add(this.tabManifests);
            this.tabControl1.Controls.Add(this.tabLogs);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(630, 511);
            this.tabControl1.TabIndex = 0;
            // 
            // tabExplorer
            // 
            this.tabExplorer.Controls.Add(this.listFiles);
            this.tabExplorer.Location = new System.Drawing.Point(4, 22);
            this.tabExplorer.Name = "tabExplorer";
            this.tabExplorer.Padding = new System.Windows.Forms.Padding(3);
            this.tabExplorer.Size = new System.Drawing.Size(622, 485);
            this.tabExplorer.TabIndex = 0;
            this.tabExplorer.Text = "Explorer";
            this.tabExplorer.UseVisualStyleBackColor = true;
            // 
            // listFiles
            // 
            this.listFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colSize,
            this.colMd5,
            this.colModified});
            this.listFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listFiles.FullRowSelect = true;
            this.listFiles.HideSelection = false;
            this.listFiles.Location = new System.Drawing.Point(3, 3);
            this.listFiles.MultiSelect = false;
            this.listFiles.Name = "listFiles";
            this.listFiles.Size = new System.Drawing.Size(616, 479);
            this.listFiles.TabIndex = 0;
            this.listFiles.UseCompatibleStateImageBehavior = false;
            this.listFiles.View = System.Windows.Forms.View.Details;
            this.listFiles.DoubleClick += new System.EventHandler(this.listFiles_DoubleClick);
            // 
            // colName
            // 
            this.colName.Text = "Name"; this.colName.Width = 250;
            // colSize
            this.colSize.Text = "Size"; this.colSize.Width = 100;
            // colMd5
            this.colMd5.Text = "MD5"; this.colMd5.Width = 200;
            // colModified
            this.colModified.Text = "Last Modified"; this.colModified.Width = 150;
            // 
            // tabManifests
            // 
            this.tabManifests.Controls.Add(this.btnViewChangelogs);
            this.tabManifests.Controls.Add(this.listManifests);
            this.tabManifests.Location = new System.Drawing.Point(4, 22);
            this.tabManifests.Name = "tabManifests";
            this.tabManifests.Padding = new System.Windows.Forms.Padding(3);
            this.tabManifests.Size = new System.Drawing.Size(622, 485);
            this.tabManifests.TabIndex = 1;
            this.tabManifests.Text = "Manifests";
            this.tabManifests.UseVisualStyleBackColor = true;
            // 
            // btnViewChangelogs
            // 
            this.btnViewChangelogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewChangelogs.Location = new System.Drawing.Point(458, 449);
            this.btnViewChangelogs.Name = "btnViewChangelogs";
            this.btnViewChangelogs.Size = new System.Drawing.Size(156, 28);
            this.btnViewChangelogs.TabIndex = 1;
            this.btnViewChangelogs.Text = "View All Changelogs";
            this.btnViewChangelogs.UseVisualStyleBackColor = true;
            this.btnViewChangelogs.Click += new System.EventHandler(this.btnViewChangelogs_Click);
            // 
            // listManifests
            // 
            this.listManifests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colVer,
            this.colFileCount,
            this.colHasChangelog});
            this.listManifests.Dock = System.Windows.Forms.DockStyle.Top;
            this.listManifests.FullRowSelect = true;
            this.listManifests.HideSelection = false;
            this.listManifests.Location = new System.Drawing.Point(3, 3);
            this.listManifests.MultiSelect = false;
            this.listManifests.Name = "listManifests";
            this.listManifests.Size = new System.Drawing.Size(616, 440);
            this.listManifests.TabIndex = 0;
            this.listManifests.UseCompatibleStateImageBehavior = false;
            this.listManifests.View = System.Windows.Forms.View.Details;
            this.listManifests.DoubleClick += new System.EventHandler(this.listManifests_DoubleClick);
            // 
            // colVer
            // 
            this.colVer.Text = "Version"; this.colVer.Width = 120;
            // colFileCount
            this.colFileCount.Text = "Files"; this.colFileCount.Width = 80;
            // colHasChangelog
            this.colHasChangelog.Text = "Changelog?"; this.colHasChangelog.Width = 100;
            // 
            // tabLogs
            // 
            this.tabLogs.Controls.Add(this.btnCreateSnapshot);
            this.tabLogs.Controls.Add(this.btnOpenLog);
            this.tabLogs.Controls.Add(this.listLogs);
            this.tabLogs.Location = new System.Drawing.Point(4, 22);
            this.tabLogs.Name = "tabLogs";
            this.tabLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogs.Size = new System.Drawing.Size(622, 485);
            this.tabLogs.TabIndex = 2;
            this.tabLogs.Text = "Logs / Snapshot";
            this.tabLogs.UseVisualStyleBackColor = true;
            // 
            // btnCreateSnapshot
            // 
            this.btnCreateSnapshot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCreateSnapshot.Location = new System.Drawing.Point(8, 449);
            this.btnCreateSnapshot.Name = "btnCreateSnapshot";
            this.btnCreateSnapshot.Size = new System.Drawing.Size(162, 28);
            this.btnCreateSnapshot.TabIndex = 2;
            this.btnCreateSnapshot.Text = "Create Diagnostic Snapshot";
            this.btnCreateSnapshot.UseVisualStyleBackColor = true;
            this.btnCreateSnapshot.Click += new System.EventHandler(this.btnCreateSnapshot_Click);
            // 
            // btnOpenLog
            // 
            this.btnOpenLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenLog.Location = new System.Drawing.Point(466, 449);
            this.btnOpenLog.Name = "btnOpenLog";
            this.btnOpenLog.Size = new System.Drawing.Size(148, 28);
            this.btnOpenLog.TabIndex = 1;
            this.btnOpenLog.Text = "Open selected";
            this.btnOpenLog.UseVisualStyleBackColor = true;
            this.btnOpenLog.Click += new System.EventHandler(this.btnOpenLog_Click);
            // 
            // listLogs
            // 
            this.listLogs.Dock = System.Windows.Forms.DockStyle.Top;
            this.listLogs.FormattingEnabled = true;
            this.listLogs.Location = new System.Drawing.Point(3, 3);
            this.listLogs.Name = "listLogs";
            this.listLogs.Size = new System.Drawing.Size(616, 446);
            this.listLogs.TabIndex = 0;
            // 
            // DiagnosticsExplorerForm
            // 
            this.ClientSize = new System.Drawing.Size(884, 511);
            this.Controls.Add(this.splitContainer1);
            this.Name = "DiagnosticsExplorerForm";
            this.Text = "Diagnostics Explorer";
            this.Load += new System.EventHandler(this.DiagnosticsExplorerForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabExplorer.ResumeLayout(false);
            this.tabManifests.ResumeLayout(false);
            this.tabLogs.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
