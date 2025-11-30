using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Security.Cryptography;
using Dh.AppLauncher.CoreEnvironment;
using Dh.AppLauncher.Http;
using Dh.AppLauncher.Logging;
using Dh.AppLauncher.Manifest;

namespace Dh.AppLauncher.Update
{
    public sealed class UpdateAvailableEventArgs : EventArgs
    {
        public string CurrentVersion { get; private set; }
        public string NewVersion { get; private set; }
        public UpdateAvailableEventArgs(string currentVersion, string newVersion){ CurrentVersion=currentVersion; NewVersion=newVersion; }
    }
    public sealed class UpdateProgressEventArgs : EventArgs
    {
        public string FileName { get; private set; } public long BytesReceived { get; private set; } public long? TotalBytes { get; private set; }
        public UpdateProgressEventArgs(string fileName, long bytesReceived, long? totalBytes){ FileName=fileName; BytesReceived=bytesReceived; TotalBytes=totalBytes; }
    }
    public sealed class UpdateCompletedEventArgs : EventArgs
    {
        public string NewVersion { get; private set; } public UpdateCompletedEventArgs(string newVersion){ NewVersion=newVersion; }
    }
    public sealed class UpdateSummaryEventArgs : EventArgs
    {
        public string NewVersion { get; private set; } public int TotalFiles { get; private set; } public bool IsDryRun { get; private set; }
        public IList<string> UnchangedFiles { get; private set; } public IList<string> ReusedFiles { get; private set; } public IList<string> DownloadFiles { get; private set; }
        public UpdateSummaryEventArgs(string v,int total,bool dry,IList<string> un,IList<string> re,IList<string> dl){NewVersion=v;TotalFiles=total;IsDryRun=dry;UnchangedFiles=un;ReusedFiles=re;DownloadFiles=dl;}
    }
    public sealed class ChangedFilesSummaryEventArgs : EventArgs
    {
        public string NewVersion { get; private set; } public IList<string> ChangedFiles { get; private set; } public bool IsDryRun { get; private set; }
        public ChangedFilesSummaryEventArgs(string v, IList<string> list, bool dry){NewVersion=v;ChangedFiles=list;IsDryRun=dry;}
    }
    public sealed class SummaryChangedFilesEventArgs : EventArgs
    {
        public string NewVersion { get; private set; } public IList<string> ChangedFiles { get; private set; } public bool IsDryRun { get; private set; }
        public long? TotalPlannedDownloadBytes { get; private set; } public long? KnownChangedBytes { get; private set; }
        public SummaryChangedFilesEventArgs(string v, IList<string> files, bool dry, long? totalPlan, long? knownChanged){ NewVersion=v; ChangedFiles=files; IsDryRun=dry; TotalPlannedDownloadBytes=totalPlan; KnownChangedBytes=knownChanged; }
    }
    public sealed class UpdateCheckResult
    {
        public bool CheckSucceeded { get; set; } public bool UpdateApplied { get; set; } public string CurrentVersion { get; set; } public string NewVersion { get; set; }
        public UpdateEnforcementLevel EnforcementLevel { get; set; }
    }
    public sealed class UpdateOptions
    {
        public string[] ManifestUrls { get; set; } public int KeepVersions { get; set; } public int MaxParallelDownloads { get; set; } public bool DryRunOnly { get; set; }
        public int MaxUpdateAttemptsPerVersion { get; set; } public int FailedVersionRetryMinutes { get; set; } public bool AllowDowngrade { get; set; }
        public UpdateEnforcementLevel DefaultUpdateLevel { get; set; }
        public UpdateOptions(){ ManifestUrls=new string[0]; KeepVersions=5; MaxParallelDownloads=3; DryRunOnly=false; MaxUpdateAttemptsPerVersion=3; FailedVersionRetryMinutes=60; AllowDowngrade=false; DefaultUpdateLevel=UpdateEnforcementLevel.Silent; }
    }
    internal sealed class DownloadTaskInfo { public string FileName; public string TargetPath; public string ExpectedMd5; public string[] Urls; public long? PlannedSize; }
    internal sealed class ManifestFetchResult { public UpdateManifest Manifest; public string RawJson; public string SourceUrl; }

    public sealed class UpdateManager
    {
        private readonly AppEnvironment _env; private readonly HttpClient _httpClient; private readonly UpdateOptions _options; private readonly string _clientId; private readonly string[] _clientGroups; private readonly string[] _clientTags;
        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable; public event EventHandler<UpdateProgressEventArgs> UpdateProgress; public event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;
        public event EventHandler<UpdateSummaryEventArgs> UpdateSummaryAvailable; public event EventHandler<ChangedFilesSummaryEventArgs> ChangedFilesSummaryAvailable; public event EventHandler<SummaryChangedFilesEventArgs> SummaryChangedFilesAvailable;

        public UpdateManager(AppEnvironment env, UpdateOptions options){ if (env==null) throw new ArgumentNullException("env"); _env=env; _options=options ?? new UpdateOptions(); _httpClient=HttpClientFactory.Client; _clientId=env.GetClientId(); _clientGroups=env.GetClientGroups(); _clientTags=env.GetClientTags(); }

        public async Task<UpdateCheckResult> CheckAndUpdateAsync(CancellationToken cancellationToken)
        {
            string currentVersion=null, newVersion=null; ManifestFetchResult selected=null;
            var result = new UpdateCheckResult { CheckSucceeded=false, UpdateApplied=false, EnforcementLevel=UpdateEnforcementLevel.Silent };
            try
            {
                // Dọn rác các thư mục staging còn sót lại do update trước đó bị dừng.
                _env.CleanupStagingVersionFolders();

                currentVersion = _env.GetActiveVersion();
                if (!string.IsNullOrWhiteSpace(currentVersion))
                {
                    var activeFolder = _env.GetVersionFolder(currentVersion);
                    if (!System.IO.Directory.Exists(activeFolder))
                    {
                        // Active version trỏ tới thư mục không tồn tại (có thể do người dùng xóa nhầm). Không crash, chỉ reset.
                        LogManager.Warn("Active version folder missing: " + activeFolder + ". Resetting currentVersion to null.");
                        currentVersion = null;
                    }
                }

                selected = await DownloadManifestAsync(cancellationToken).ConfigureAwait(false);
                if (selected == null || selected.Manifest == null){ LogManager.Info("No valid manifest. Skip update."); result.CheckSucceeded=false; return result; }
                var manifest = selected.Manifest; newVersion = manifest.Version; result.NewVersion=newVersion; result.CurrentVersion=currentVersion; result.EnforcementLevel = ResolveEnforcementLevel(manifest.UpdateLevel, _options.DefaultUpdateLevel);
                if (string.IsNullOrWhiteSpace(newVersion)){ LogManager.Info("Manifest empty version. Skip."); result.CheckSucceeded=false; return result; }
                try { _env.SaveVersionManifest(newVersion, selected.RawJson); LogManager.Info("Saved latest manifest snapshot for "+newVersion+"."); } catch (Exception exSave){ LogManager.Warn("Save latest manifest failed: "+exSave.Message); }

                if (!string.IsNullOrWhiteSpace(currentVersion))
                {
                    Version cur, target;
                    if (Version.TryParse(currentVersion, out cur) && Version.TryParse(newVersion, out target))
                    { if (target <= cur && !_options.AllowDowngrade){ LogManager.Info("Latest version ("+newVersion+") is not newer than current ("+currentVersion+")."); result.CheckSucceeded=true; result.UpdateApplied=false; return result; } }
                }

                if (!_env.CanAttemptUpdateVersion(newVersion, _options.MaxUpdateAttemptsPerVersion, _options.FailedVersionRetryMinutes))
                { LogManager.Warn("Retry policy blocks update for "+newVersion+"."); result.CheckSucceeded=false; return result; }

                var ua = UpdateAvailable; if (ua!=null) ua(this, new UpdateAvailableEventArgs(currentVersion ?? "(null)", newVersion));
                var targetVersionFolder = _env.GetVersionFolder(newVersion); Directory.CreateDirectory(targetVersionFolder);

                var unchangedFiles = new List<string>(); var reusedFiles = new List<string>(); var downloadFiles = new List<DownloadTaskInfo>();

                foreach (var kv in manifest.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var name = kv.Key; var entry = kv.Value;
                    // Đảm bảo file đích luôn nằm bên dưới thư mục version được phép.
                    var targetPath = GetSafePath(targetVersionFolder, name);
                    var dir = Path.GetDirectoryName(targetPath); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    var expectedMd5 = (entry.Md5 ?? string.Empty).ToLowerInvariant();

                    if (File.Exists(targetPath))
                    {
                        var curMd5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(targetPath);
                        if (!string.IsNullOrEmpty(curMd5) && string.Equals(curMd5, expectedMd5, StringComparison.OrdinalIgnoreCase))
                        { unchangedFiles.Add(name); continue; }
                    }

                    if (!_options.DryRunOnly && TryReuseFromOtherVersion(targetPath, name, expectedMd5))
                    { reusedFiles.Add(name); continue; }

                    string[] urls = (entry.Urls!=null && entry.Urls.Length>0) ? entry.Urls : (!string.IsNullOrWhiteSpace(entry.Url) ? new string[]{entry.Url} : new string[0]);
                    downloadFiles.Add(new DownloadTaskInfo{ FileName=name, TargetPath=targetPath, ExpectedMd5=expectedMd5, Urls=urls });
                }

                long? totalPlanned = await EstimatePlannedBytesAsync(downloadFiles, cancellationToken).ConfigureAwait(false);
                var changedList = new List<string>(); changedList.AddRange(reusedFiles); changedList.AddRange(downloadFiles.ConvertAll(d=>d.FileName)); changedList = changedList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var us = UpdateSummaryAvailable; if (us!=null) us(this, new UpdateSummaryEventArgs(newVersion, manifest.Files.Count, _options.DryRunOnly, unchangedFiles, reusedFiles, downloadFiles.ConvertAll(d=>d.FileName)));
                var cf = ChangedFilesSummaryAvailable; if (cf!=null) cf(this, new ChangedFilesSummaryEventArgs(newVersion, changedList, _options.DryRunOnly));
                var scf = SummaryChangedFilesAvailable; if (scf!=null) scf(this, new SummaryChangedFilesEventArgs(newVersion, changedList, _options.DryRunOnly, totalPlanned, totalPlanned));

                if (_options.DryRunOnly){ result.CheckSucceeded=true; result.UpdateApplied=false; return result; }

                if (downloadFiles.Count > 0)
                {
                    int maxPar = _options.MaxParallelDownloads > 0 ? _options.MaxParallelDownloads : 3;
                    var sem = new System.Threading.SemaphoreSlim(maxPar);
                    var tasks = new List<Task>();
                    foreach (var it in downloadFiles)
                    {
                        await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                        var t = Task.Run(async ()=>{ try { await DownloadFileWithMirrorsAsync(it, cancellationToken).ConfigureAwait(false); } finally { sem.Release(); }}, cancellationToken);
                        tasks.Add(t);
                    }
                    try { await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false); }
                    catch (Exception ex){ LogManager.Error("One or more file downloads failed. Aborting.", ex); _env.RegisterUpdateAttempt(newVersion, false, "DOWNLOAD_ERROR"); result.CheckSucceeded=false; result.UpdateApplied=false; return result; }
                }

                _env.SetActiveVersion(newVersion); _env.CleanupOldVersions(_options.KeepVersions); _env.RegisterUpdateAttempt(newVersion, true, null);
                var uc = UpdateCompleted; if (uc!=null) uc(this, new UpdateCompletedEventArgs(newVersion));
                result.CheckSucceeded=true; result.UpdateApplied=true; return result;
            }
            catch (Exception ex)
            { LogManager.Error("CheckAndUpdateAsync failed.", ex); if (!string.IsNullOrWhiteSpace(newVersion)) _env.RegisterUpdateAttempt(newVersion, false, "GENERAL_ERROR"); result.CheckSucceeded=false; result.UpdateApplied=false; return result; }
        }


        private static string[] GetZipUrlList(UpdateManifest manifest)
        {
            if (manifest == null) return new string[0];
            if (manifest.ZipUrls != null && manifest.ZipUrls.Length > 0) return manifest.ZipUrls;
            if (manifest.Urls != null && manifest.Urls.Length > 0) return manifest.Urls;
            return new string[0];
        }

        /// <summary>
        /// Xây dựng đường dẫn an toàn bên dưới một root cố định, tránh path traversal kiểu "..\\".
        /// Nếu relativePath chui ra ngoài root thì ném InvalidOperationException.
        /// </summary>
        private static string GetSafePath(string root, string relativePath)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException("root");
            if (relativePath == null) relativePath = string.Empty;

            // Chuẩn hóa: thay '/' thành separator hiện tại, tránh mix lẫn.
            relativePath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);

            var rootFull = System.IO.Path.GetFullPath(root);
            var combined = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootFull, relativePath));

            // Đảm bảo combined nằm dưới rootFull (hoặc chính nó) để tránh ghi file ra ngoài vùng cho phép.
            if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Relative path escapes root: " + relativePath);

            if (combined.Length > rootFull.Length)
            {
                var ch = combined[rootFull.Length];
                if (ch != System.IO.Path.DirectorySeparatorChar && ch != System.IO.Path.AltDirectorySeparatorChar)
                    throw new InvalidOperationException("Relative path escapes root: " + relativePath);
            }

            return combined;
        }

        private static bool VerifySha512Hex(string filePath, string expectedHex)
        {
            if (string.IsNullOrWhiteSpace(expectedHex)) return true;
            var expected = expectedHex.Trim().ToLowerInvariant();
            using (var sha = SHA512.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                var actual = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ComputeMd5Hex(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private async Task DownloadZipToFileAsync(string url, string targetPath, CancellationToken ct)
        {
            using (var resp = await HttpClientFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using (var input = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = File.Create(targetPath))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<bool> TryApplyZipPackageAsync(UpdateManifest manifest, string version, CancellationToken ct)
        {
            LogManager.Info("Starting ZIP package apply for version " + version + ".");
            var zipUrls = GetZipUrlList(manifest);
            if (zipUrls == null || zipUrls.Length == 0)
            {
                LogManager.Info("No ZIP URLs found for version " + version + ", skipping ZIP path.");
                return false;
            }


            var versionsRoot = _env.GetVersionsRoot();
            Directory.CreateDirectory(versionsRoot);

            var finalRoot = _env.GetVersionFolder(version);
            var stagingRoot = finalRoot + ".__zipstaging";
            if (Directory.Exists(stagingRoot))
            {
                try { Directory.Delete(stagingRoot, true); } catch { }
            }
            Directory.CreateDirectory(stagingRoot);

            string tempZip = Path.Combine(Path.GetTempPath(), "DhLauncher_" + version + "_" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                if (File.Exists(tempZip))
                {
                    try { File.Delete(tempZip); } catch { }
                }

                Exception last = null;
                bool downloaded = false;
                foreach (var url in zipUrls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    try
                    {
                        LogManager.Info("Downloading ZIP package for version " + version + " from " + url);
                        await DownloadZipToFileAsync(url, tempZip, ct).ConfigureAwait(false);
                        downloaded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        LogManager.Error("Download ZIP failed from " + url + " for version " + version, ex);
                    }
                }

                if (!downloaded)
                {
                    LogManager.Warn("All ZIP URLs failed for version " + version + ". Fallback to per-file if possible.");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(manifest.Sha512))
                {
                    try
                    {
                        LogManager.Info("Verifying ZIP sha512 for version " + version + ".");
                        if (!VerifySha512Hex(tempZip, manifest.Sha512))
                        {
                            LogManager.Warn("ZIP sha512 mismatch for version " + version + ". Fallback to per-file.");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error("Error verifying ZIP sha512 for version " + version, ex);
                        return false;
                    }
                }

                try
                {
                    LogManager.Info("Unzipping ZIP package for version " + version + ".");
                    using (var zip = ZipFile.OpenRead(tempZip))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            ct.ThrowIfCancellationRequested();
                            // Sử dụng GetSafePath để chặn path traversal trong entry.FullName.
                            var destPath = GetSafePath(stagingRoot, entry.FullName);

                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                // directory
                                Directory.CreateDirectory(destPath);
                                continue;
                            }

                            var dir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            entry.ExtractToFile(destPath, overwrite: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error("Unzip failed for version " + version, ex);
                    return false;
                }

                // Verify MD5 từng file theo manifest.Files
                LogManager.Info("Verifying MD5 of extracted files for version " + version + ".");
                if (manifest.Files != null)
                {
                    foreach (var kv in manifest.Files)
                    {
                        ct.ThrowIfCancellationRequested();
                        var relPath = kv.Key;
                        var entry = kv.Value;
                        if (entry == null || string.IsNullOrWhiteSpace(entry.Md5)) continue;

                        var filePath = GetSafePath(stagingRoot, relPath);
                        if (!File.Exists(filePath))
                        {
                            LogManager.Warn("File from ZIP missing in staging: " + relPath);
                            return false;
                        }

                        var md5 = ComputeMd5Hex(filePath);
                        if (!string.Equals(md5, entry.Md5, StringComparison.OrdinalIgnoreCase))
                        {
                            LogManager.Warn("MD5 mismatch for file " + relPath + " in ZIP. Expected=" + entry.Md5 + ", Actual=" + md5);
                            return false;
                        }
                    }
                }

                // Promote staging -> final
                try
                {
                    LogManager.Info("Promoting ZIP staging folder to final version folder for " + version + ".");
                    if (Directory.Exists(finalRoot))
                    {
                        try { Directory.Delete(finalRoot, true); } catch { }
                    }
                    Directory.Move(stagingRoot, finalRoot);
                }
                catch (Exception ex)
                {
                    LogManager.Error("Failed to promote ZIP staging folder for version " + version, ex);
                    return false;
                }

                LogManager.Info("ZIP package applied successfully for version " + version);
                return true;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempZip))
                    {
                        File.Delete(tempZip);
                        LogManager.Info("Cleaned up temporary ZIP file for version " + version + ".");
                    }
                }
                catch { }
                try
                {
                    if (Directory.Exists(stagingRoot))
                    {
                        Directory.Delete(stagingRoot, true);
                        LogManager.Info("Cleaned up ZIP staging folder for version " + version + ".");
                    }
                }
                catch { }
            }
        }

        private UpdateEnforcementLevel ResolveEnforcementLevel(string manifestLevel, UpdateEnforcementLevel defaultLevel)
        {
            if (!string.IsNullOrWhiteSpace(manifestLevel))
            {
                switch (manifestLevel.Trim().ToLowerInvariant())
                {
                    case "silent": return UpdateEnforcementLevel.Silent;
                    case "ask": return UpdateEnforcementLevel.AskUser;
                    case "force": return UpdateEnforcementLevel.Force;
                }
            }
            return defaultLevel;
        }

        private async Task<long?> EstimatePlannedBytesAsync(List<DownloadTaskInfo> items, CancellationToken ct)
        {
            if (items==null || items.Count==0) return 0;
            long total = 0; bool anyKnown=false;
            foreach (var it in items)
            {
                long? size = null;
                foreach (var url in it.Urls)
                {
                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Head, url))
                        using (var resp = await HttpClientFactory.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                        { if ((int)resp.StatusCode>=200 && (int)resp.StatusCode<400){ if (resp.Content!=null && resp.Content.Headers.ContentLength.HasValue){ size = resp.Content.Headers.ContentLength.Value; break; } } }
                        using (var resp2 = await HttpClientFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                        { if (resp2.Content!=null && resp2.Content.Headers.ContentLength.HasValue){ size = resp2.Content.Headers.ContentLength.Value; break; } }
                    } catch {}
                }
                it.PlannedSize = size; if (size.HasValue){ total += size.Value; anyKnown=true; }
            }
            return anyKnown ? (long?)total : null;
        }

        private async Task<ManifestFetchResult> DownloadManifestAsync(CancellationToken ct)
        {
            if (_options.ManifestUrls==null || _options.ManifestUrls.Length==0){ LogManager.Warn("No manifest URLs."); return null; }
            var list = new List<Tuple<UpdateManifest, Version, string, string>>();
            foreach (var url in _options.ManifestUrls)
            {
                try
                {
                    var resp = await HttpClientFactory.Client.GetAsync(url, ct).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    UpdateManifest m;
                    var ctype = resp.Content.Headers.ContentType!=null ? resp.Content.Headers.ContentType.MediaType : string.Empty;
                    if (ctype.IndexOf("xml", StringComparison.OrdinalIgnoreCase)>=0 || text.TrimStart().StartsWith("<")) m = UpdateManifest.FromXml(text);
                    else m = UpdateManifest.FromJson(text);
                    if (m==null || string.IsNullOrWhiteSpace(m.Version)) continue;
                    if (!IsManifestApplicableToClient(m)) { LogManager.Info("Manifest "+m.Version+" skipped due to client_match."); continue; }
                    Version ver; if (!Version.TryParse(m.Version, out ver)) continue;
                    list.Add(new Tuple<UpdateManifest, Version, string, string>(m, ver, url, text));
                }
                catch (Exception ex){ LogManager.Error("Manifest fetch failed from "+url, ex); }
            }
            if (list.Count==0) return null;
            list.Sort((a,b)=>b.Item2.CompareTo(a.Item2));
            var best = list[0];
            return new ManifestFetchResult{ Manifest = best.Item1, RawJson = best.Item4, SourceUrl = best.Item3 };
        }

        private bool IsManifestApplicableToClient(UpdateManifest manifest)
        {
            var match = manifest.ClientMatch;
            if (match == null || match.Rules == null || match.Rules.Length == 0) return true;

            var machineName = System.Environment.MachineName;
            var osVer = System.Environment.OSVersion != null ? System.Environment.OSVersion.ToString() : string.Empty;

            if (string.IsNullOrWhiteSpace(_clientId) && string.IsNullOrWhiteSpace(machineName) && string.IsNullOrWhiteSpace(osVer))
                return false;

            foreach (var rule in match.Rules)
            {
                bool ok = true;

                if (rule.ClientIds != null && rule.ClientIds.Length > 0)
                {
                    if (string.IsNullOrWhiteSpace(_clientId)) ok = false;
                    else
                    {
                        bool any = false;
                        foreach (var id in rule.ClientIds)
                        {
                            if (string.Equals(id, _clientId, StringComparison.OrdinalIgnoreCase)) { any = true; break; }
                        }
                        if (!any) ok = false;
                    }
                }

                if (ok && rule.MachineNames != null && rule.MachineNames.Length > 0)
                {
                    bool any = false;
                    foreach (var n in rule.MachineNames)
                    {
                        if (string.Equals(n, machineName, StringComparison.OrdinalIgnoreCase)) { any = true; break; }
                    }
                    if (!any) ok = false;
                }

                if (ok && rule.OsVersions != null && rule.OsVersions.Length > 0)
                {
                    bool any = false;
                    foreach (var v in rule.OsVersions)
                    {
                        if (!string.IsNullOrWhiteSpace(v) && osVer.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0) { any = true; break; }
                    }
                    if (!any) ok = false;
                }

                if (ok && rule.Groups != null && rule.Groups.Length > 0 && _clientGroups != null && _clientGroups.Length > 0)
                {
                    bool any = false;
                    foreach (var g in rule.Groups)
                    {
                        foreach (var cg in _clientGroups)
                        {
                            if (string.Equals(g, cg, StringComparison.OrdinalIgnoreCase)) { any = true; break; }
                        }
                        if (any) break;
                    }
                    if (!any) ok = false;
                }

                if (ok && rule.Tags != null && rule.Tags.Length > 0 && _clientTags != null && _clientTags.Length > 0)
                {
                    bool any = false;
                    foreach (var t in rule.Tags)
                    {
                        foreach (var ct in _clientTags)
                        {
                            if (string.Equals(t, ct, StringComparison.OrdinalIgnoreCase)) { any = true; break; }
                        }
                        if (any) break;
                    }
                    if (!any) ok = false;
                }

                if (ok) return true;
            }
            return false;
        }

        private bool TryReuseFromOtherVersion(string targetPath, string fileName, string expectedMd5)
        {
            try
            {
                var versions = _env.GetInstalledVersions();
                foreach (var v in versions)
                {
                    var src = GetSafePath(_env.GetVersionFolder(v), fileName);
                    if (!File.Exists(src)) continue;
                    var md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(src);
                    if (!string.IsNullOrEmpty(md5) && string.Equals(md5, expectedMd5, StringComparison.OrdinalIgnoreCase))
                    { Directory.CreateDirectory(Path.GetDirectoryName(targetPath)); File.Copy(src, targetPath, true); LogManager.Info("Reused file "+fileName+" from version "+v); return true; }
                }
            } catch (Exception ex){ LogManager.Error("Reuse check error for "+fileName, ex); }
            return false;
        }

        private async Task DownloadFileWithMirrorsAsync(DownloadTaskInfo info, CancellationToken ct)
        {
            if (info==null) throw new ArgumentNullException("info");
            if (info.Urls==null || info.Urls.Length==0) throw new ArgumentException("No URLs for "+info.FileName);
            Exception last=null;
            foreach (var url in info.Urls)
            {
                try
                {
                    await DownloadFileOnceAsync(url, info, ct).ConfigureAwait(false);
                    var md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(info.TargetPath);
                    if (!string.Equals(md5, info.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                    { LogManager.Error("MD5 mismatch for "+info.FileName+" from "+url+"."); last = new InvalidOperationException("MD5 mismatch"); continue; }
                    return;
                } catch (Exception ex){ last=ex; LogManager.Error("Download failed from "+url+" for "+info.FileName, ex); }
            }
            if (last!=null) throw last;
        }

        private async Task DownloadFileOnceAsync(string url, DownloadTaskInfo info, CancellationToken ct)
        {
            using (var resp = await HttpClientFactory.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                var total = resp.Content.Headers.ContentLength;
                var tmp = info.TargetPath + ".download";
                Directory.CreateDirectory(Path.GetDirectoryName(tmp));
                using (var input = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = File.Create(tmp))
                {
                    byte[] buf = new byte[81920]; long readTotal=0; int read;
                    while ((read = await input.ReadAsync(buf,0,buf.Length,ct).ConfigureAwait(false))>0)
                    {
                        await output.WriteAsync(buf,0,read,ct).ConfigureAwait(false);
                        readTotal += read;
                        var h = UpdateProgress; if (h!=null) h(this, new UpdateProgressEventArgs(info.FileName, readTotal, total));
                    }
                }
                File.Copy(tmp, info.TargetPath, true); File.Delete(tmp);
            }
        }
        /// <summary>
        /// Thực hiện kiểm tra cập nhật ở chế độ dry-run (không chép file, không đổi active version).
        /// Sử dụng cho UI muốn xem trước: version mới, mức độ enforcement, danh sách file thay đổi…
        /// </summary>
        public System.Threading.Tasks.Task<UpdateCheckResult> CheckOnlyAsync(System.Threading.CancellationToken cancellationToken)
        {
            // Sao lưu trạng thái DryRunOnly hiện tại để không ảnh hưởng tới lần gọi sau.
            bool backup = _options.DryRunOnly;
            try
            {
                _options.DryRunOnly = true;
                // Tận dụng toàn bộ luồng CheckAndUpdateAsync, nhưng luôn ở chế độ dry-run.
                return CheckAndUpdateAsync(cancellationToken);
            }
            finally
            {
                _options.DryRunOnly = backup;
            }
        }

        /// <summary>
        /// Thực hiện kiểm tra và áp dụng bản cập nhật mới nhất theo options hiện tại (DryRunOnly = false).
        /// Nếu không có bản mới, sẽ chỉ trả về kết quả check mà không chép file.
        /// </summary>
        public System.Threading.Tasks.Task<UpdateCheckResult> ApplyLatestAsync(System.Threading.CancellationToken cancellationToken)
        {
            bool backup = _options.DryRunOnly;
            try
            {
                _options.DryRunOnly = false;
                // Cho phép CheckAndUpdateAsync áp dụng update thực sự nếu có.
                return CheckAndUpdateAsync(cancellationToken);
            }
            finally
            {
                _options.DryRunOnly = backup;
            }
        }

    }
}
