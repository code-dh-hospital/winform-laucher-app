using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Compression;
using Dh.AppLauncher.CoreEnvironment;
using Dh.AppLauncher.Manifest;

namespace Dh.Launcher.WinFormsTest
{
    public partial class DiagnosticsExplorerForm : Form
    {
        private string _root;
        private string _manifestsRoot;
        private string _logsRoot;
        private string _clientId;

        public DiagnosticsExplorerForm(){ InitializeComponent(); }

        private void DiagnosticsExplorerForm_Load(object sender, EventArgs e)
        {
            var env = AppEnvironment.Initialize("Dh.Updater.SampleApp");
            _root = env.LocalRoot; _manifestsRoot = env.VersionManifestsRoot; _logsRoot = env.LogsRoot; _clientId = env.GetClientId();
            this.Text = "Diagnostics Explorer - ClientId: " + (_clientId ?? "(null)");
            BuildTree(); LoadManifests(); LoadLogs();
        }

        private void BuildTree()
        {
            treeFolders.Nodes.Clear();
            var rootNode = new TreeNode(Path.GetFileName(_root)) { Tag = _root };
            treeFolders.Nodes.Add(rootNode);
            AddSubFolders(rootNode, _root, depth: 4);
            rootNode.Expand();
        }
        private void AddSubFolders(TreeNode parent, string path, int depth)
        {
            if (depth <= 0) return;
            try{ foreach (var dir in Directory.GetDirectories(path)){ var node = new TreeNode(Path.GetFileName(dir)) { Tag = dir }; parent.Nodes.Add(node); AddSubFolders(node, dir, depth - 1); } } catch { }
        }
        private void treeFolders_AfterSelect(object sender, TreeViewEventArgs e){ var path = e.Node.Tag as string; ShowFiles(path); }

        private void ShowFiles(string path)
        {
            listFiles.Items.Clear();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
            foreach (var f in Directory.GetFiles(path))
            {
                try
                {
                    var fi = new FileInfo(f);
                    string md5 = string.Empty; try { md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(f); } catch { }
                    var item = new ListViewItem(new [] { Path.GetFileName(f), fi.Length.ToString(), md5 ?? "", fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") }) { Tag = f };
                    listFiles.Items.Add(item);
                } catch { }
            }
        }

        private void listFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listFiles.SelectedItems.Count == 0) return;
            var path = listFiles.SelectedItems[0].Tag as string; if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                string text = ""; using (var reader = new StreamReader(path, Encoding.UTF8, true)){ text = reader.ReadToEnd(); }
                using (var tv = new TextViewerForm()){ tv.LoadText(Path.GetFileName(path), text); tv.ShowDialog(this); }
            } catch (Exception ex) { MessageBox.Show("Cannot open file: " + ex.Message); }
        }

        private void LoadManifests()
        {
            listManifests.Items.Clear();
            try
            {
                foreach (var f in Directory.GetFiles(_manifestsRoot, "*.json"))
                {
                    try
                    {
                        var txt = File.ReadAllText(f, Encoding.UTF8);
                        var m = UpdateManifest.FromJson(txt);
                        int fileCount = m!=null && m.Files!=null ? m.Files.Count : 0;
                        string ver = m!=null ? (m.Version ?? Path.GetFileNameWithoutExtension(f)) : Path.GetFileNameWithoutExtension(f);
                        var item = new ListViewItem(new [] { ver, fileCount.ToString(), string.IsNullOrWhiteSpace(m!=null?m.Changelog:null) ? "No" : "Yes" }) { Tag = f };
                        listManifests.Items.Add(item);
                    } catch { }
                }
            } catch { }
        }

        private void listManifests_DoubleClick(object sender, EventArgs e)
        {
            if (listManifests.SelectedItems.Count == 0) return;
            var path = listManifests.SelectedItems[0].Tag as string; if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                using (var tv = new TextViewerForm()){ tv.LoadText(Path.GetFileName(path), text); tv.ShowDialog(this); }
            } catch (Exception ex) { MessageBox.Show("Cannot open manifest: " + ex.Message); }
        }

        private void btnViewChangelogs_Click(object sender, EventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                var files = Directory.GetFiles(_manifestsRoot, "*.json");
                var list = files.OrderBy(x => x).ToArray();
                foreach (var f in list)
                {
                    try
                    {
                        var txt = File.ReadAllText(f, Encoding.UTF8);
                        var m = UpdateManifest.FromJson(txt);
                        if (m != null && !string.IsNullOrWhiteSpace(m.Changelog))
                        {
                            sb.AppendLine("### " + (m.Version ?? Path.GetFileNameWithoutExtension(f)));
                            sb.AppendLine(m.Changelog);
                            sb.AppendLine();
                        }
                    } catch { }
                }
                using (var tv = new TextViewerForm()){ tv.LoadText("All Changelogs", sb.ToString()); tv.ShowDialog(this); }
            } catch (Exception ex) { MessageBox.Show("Cannot aggregate changelogs: " + ex.Message); }
        }

        private void LoadLogs()
        {
            listLogs.Items.Clear();
            try{ foreach (var f in Directory.GetFiles(_logsRoot, "*.log")){ listLogs.Items.Add(f); } } catch { }
        }

        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            if (listLogs.SelectedItem == null) return;
            var path = listLogs.SelectedItem.ToString();
            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                using (var tv = new TextViewerForm()){ tv.LoadText(Path.GetFileName(path), text); tv.ShowDialog(this); }
            } catch (Exception ex) { MessageBox.Show("Cannot open log: " + ex.Message); }
        }

        private void btnCreateSnapshot_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Zip Files (*.zip)|*.zip";
                dlg.FileName = "DiagSnapshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try { CreateDiagnosticSnapshot(dlg.FileName); MessageBox.Show("Snapshot created: " + dlg.FileName); }
                catch (Exception ex){ MessageBox.Show("Create snapshot failed: " + ex.Message); }
            }
        }

        private void CreateDiagnosticSnapshot(string zipPath)
        {
            var _rootPath = _root.TrimEnd(Path.DirectorySeparatorChar);
            int rootLen = _rootPath.Length + 1;
            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(_root, "*", SearchOption.AllDirectories))
                {
                    string relPath = file.Substring(rootLen).Replace(Path.DirectorySeparatorChar, '/');
                    var ext = Path.GetExtension(file);
                    bool isExeOrDll = string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase);

                    if (isExeOrDll)
                    {
                        string infoName = relPath + ".info.txt";
                        var entry = zip.CreateEntry(infoName, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                        {
                            var fi = new FileInfo(file);
                            string md5 = "";
                            try { md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(file); } catch { }
                            double mb = fi.Length / 1024.0 / 1024.0;
                            writer.WriteLine("Name: " + fi.Name);
                            writer.WriteLine("Extension: " + fi.Extension);
                            writer.WriteLine("SizeBytes: " + fi.Length);
                            writer.WriteLine("SizeMB: " + mb.ToString("0.###"));
                            writer.WriteLine("LastWriteTime: " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            writer.WriteLine("MD5: " + (md5 ?? ""));
                            writer.WriteLine("RelativePath: " + relPath);
                            writer.WriteLine("ClientId: " + (_clientId ?? ""));
                        }
                    }
                    else
                    {
                        var entry = zip.CreateEntry(relPath, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var fileStream = File.OpenRead(file))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }
            }
        }
    }
}
