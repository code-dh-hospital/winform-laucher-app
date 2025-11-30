using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using Newtonsoft.Json;

namespace Dh.AppLauncher.CoreEnvironment
{
    internal sealed class LauncherConfig
    {
        [JsonProperty("active_version")] public string ActiveVersion { get; set; }
        [JsonProperty("keep_versions")] public int KeepVersions { get; set; }
        [JsonProperty("auto_check_updates")] public bool AutoCheckUpdates { get; set; }
        [JsonProperty("check_interval_minutes")] public int CheckIntervalMinutes { get; set; }
        [JsonProperty("latest_manifest_urls")] public string[] LatestManifestUrls { get; set; }
        [JsonProperty("version_manifest_templates")] public string[] VersionManifestTemplates { get; set; }
        [JsonProperty("auto_repair_missing_files")] public bool AutoRepairMissingFiles { get; set; }
        [JsonProperty("max_update_attempts_per_version")] public int MaxUpdateAttemptsPerVersion { get; set; }
        [JsonProperty("failed_version_retry_minutes")] public int FailedVersionRetryMinutes { get; set; }
        [JsonProperty("allow_downgrade")] public bool AllowDowngrade { get; set; }
        [JsonProperty("skip_update_if_offline")] public bool SkipUpdateIfOffline { get; set; }
        [JsonProperty("default_update_level")] public string DefaultUpdateLevel { get; set; }

        public LauncherConfig()
        {
            KeepVersions = 5; AutoCheckUpdates = true; CheckIntervalMinutes = 30;
            LatestManifestUrls = new string[0]; VersionManifestTemplates = new string[0];
            AutoRepairMissingFiles = true; MaxUpdateAttemptsPerVersion = 3; FailedVersionRetryMinutes = 60;
            AllowDowngrade = false; SkipUpdateIfOffline = true; DefaultUpdateLevel = "silent";
        }
    }

    internal sealed class ClientIdentityInfo
    {
        [JsonProperty("client_id")] public string ClientId { get; set; }
        [JsonProperty("created_utc")] public DateTime CreatedUtc { get; set; }
        [JsonProperty("machine_name")] public string MachineName { get; set; }
        [JsonProperty("local_root")] public string LocalRoot { get; set; }
        [JsonProperty("groups")] public string[] Groups { get; set; }
        [JsonProperty("tags")] public string[] Tags { get; set; }
    }

    public sealed class AppEnvironment
    {
        private readonly string _appName, _localRoot, _versionsRoot, _configRoot, _updatesRoot, _logsRoot, _manifestsRoot;
        private readonly string _launcherConfigPath, _updateStatePath, _clientIdentityPath;

        private LauncherConfig _config; private UpdateState _updateState; private ClientIdentityInfo _clientIdentity;

        public string AppName { get { return _appName; } }
        public string LocalRoot { get { return _localRoot; } }
        public string VersionsRoot { get { return _versionsRoot; } }
        public string ConfigRoot { get { return _configRoot; } }
        public string UpdatesRoot { get { return _updatesRoot; } }
        public string LogsRoot { get { return _logsRoot; } }
        public string VersionManifestsRoot { get { return _manifestsRoot; } }

        public static AppEnvironment Initialize(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentException("AppName is required.", "appName");
            var localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var localRoot = Path.Combine(localAppData, appName);
            Directory.CreateDirectory(localRoot);
            var configRoot = Path.Combine(localRoot, "Config");
            var versionsRoot = Path.Combine(localRoot, "Versions");
            var updatesRoot = Path.Combine(localRoot, "Updates");
            var logsRoot = Path.Combine(localRoot, "Logs");
            var manifestsRoot = Path.Combine(configRoot, "manifests");
            Directory.CreateDirectory(configRoot); Directory.CreateDirectory(versionsRoot); Directory.CreateDirectory(updatesRoot);
            Directory.CreateDirectory(logsRoot); Directory.CreateDirectory(manifestsRoot);
            return new AppEnvironment(appName, localRoot, versionsRoot, configRoot, updatesRoot, logsRoot, manifestsRoot);
        }

        private AppEnvironment(string appName, string localRoot, string versionsRoot, string configRoot, string updatesRoot, string logsRoot, string manifestsRoot)
        {
            _appName=appName; _localRoot=localRoot; _versionsRoot=versionsRoot; _configRoot=configRoot; _updatesRoot=updatesRoot; _logsRoot=logsRoot; _manifestsRoot=manifestsRoot;
            _launcherConfigPath = Path.Combine(_configRoot, "launcher.json");
            _updateStatePath = Path.Combine(_configRoot, "update_state.json");
            _clientIdentityPath = Path.Combine(_configRoot, "client_identity.json");
            LoadOrCreateConfig(); LoadOrCreateUpdateState(); LoadOrCreateClientIdentity();
        }

        public string GetVersionManifestPath(string version){ return Path.Combine(_manifestsRoot, "manifest-" + version + ".json"); }
        public string GetLatestManifestPath(){ return Path.Combine(_manifestsRoot, "latest.json"); }
        public void SaveVersionManifest(string version, string rawJson){ if (string.IsNullOrWhiteSpace(version)||string.IsNullOrWhiteSpace(rawJson)) return; File.WriteAllText(GetVersionManifestPath(version), rawJson); File.WriteAllText(GetLatestManifestPath(), rawJson); }

        private void LoadOrCreateConfig()
        {
            if (File.Exists(_launcherConfigPath))
            { var json = File.ReadAllText(_launcherConfigPath); _config = JsonConvert.DeserializeObject<LauncherConfig>(json) ?? new LauncherConfig(); }
            else { _config = new LauncherConfig(); SaveConfig(); }
        }
        private void SaveConfig(){ var json = JsonConvert.SerializeObject(_config, Formatting.Indented); File.WriteAllText(_launcherConfigPath, json); }

        private void LoadOrCreateUpdateState()
        {
            if (File.Exists(_updateStatePath))
            { var json = File.ReadAllText(_updateStatePath); _updateState = JsonConvert.DeserializeObject<UpdateState>(json) ?? new UpdateState(); }
            else { _updateState = new UpdateState(); SaveUpdateState(); }
        }
        private void SaveUpdateState(){ var json = JsonConvert.SerializeObject(_updateState, Formatting.Indented); File.WriteAllText(_updateStatePath, json); }

        private void LoadOrCreateClientIdentity()
        {
            if (File.Exists(_clientIdentityPath))
            {
                try { var json = File.ReadAllText(_clientIdentityPath); _clientIdentity = JsonConvert.DeserializeObject<ClientIdentityInfo>(json); if (_clientIdentity!=null && !string.IsNullOrWhiteSpace(_clientIdentity.ClientId)) return; } catch {}
            }
            _clientIdentity = new ClientIdentityInfo();
            _clientIdentity.MachineName = System.Environment.MachineName; _clientIdentity.LocalRoot = _localRoot; _clientIdentity.CreatedUtc = DateTime.UtcNow;
            _clientIdentity.ClientId = GenerateDeterministicUuidV7(BuildHardwareSeed());
            try { var json = JsonConvert.SerializeObject(_clientIdentity, Formatting.Indented); File.WriteAllText(_clientIdentityPath, json); } catch {}
        }

        private string BuildHardwareSeed()
        {
            var sb = new StringBuilder();
            sb.Append(_appName).Append("|"); sb.Append(_localRoot).Append("|"); sb.Append(System.Environment.MachineName).Append("|");
            sb.Append(System.Environment.OSVersion != null ? System.Environment.OSVersion.ToString() : "").Append("|");
            try{ using (var mc = new ManagementClass("Win32_Processor")){ foreach (var obj in mc.GetInstances()){ var p = obj["ProcessorId"] as string; if (!string.IsNullOrEmpty(p)) { sb.Append("CPU:").Append(p).Append("|"); break; } } } } catch {}
            try{ using (var mc = new ManagementClass("Win32_BaseBoard")){ foreach (var obj in mc.GetInstances()){ var p = obj["SerialNumber"] as string; if (!string.IsNullOrEmpty(p)) { sb.Append("MB:").Append(p).Append("|"); break; } } } } catch {}
            try{ var di = new DirectoryInfo(_localRoot); sb.Append("RootCreated:").Append(di.CreationTimeUtc.ToString("o")); } catch {}
            return sb.ToString();
        }

        private static string GenerateDeterministicUuidV7(string seed)
        {
            if (seed == null) seed = string.Empty;
            var now = DateTime.UtcNow; var epoch = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc); var ms = (long)(now-epoch).TotalMilliseconds;
            var timestampBytes = new byte[8]; for (int i=0;i<8;i++){ timestampBytes[7-i]=(byte)(ms & 0xFF); ms >>= 8; }
            byte[] hash; using (var sha = SHA256.Create()){ hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed)); }
            var uuid = new byte[16];
            uuid[0]=timestampBytes[2]; uuid[1]=timestampBytes[3]; uuid[2]=timestampBytes[4]; uuid[3]=timestampBytes[5]; uuid[4]=timestampBytes[6]; uuid[5]=timestampBytes[7];
            for (int i=0;i<10;i++){ uuid[6+i]=hash[i]; }
            uuid[6] &= 0x0F; uuid[6] |= 0x70; uuid[8] &= 0x3F; uuid[8] |= 0x80;
            var guid = new Guid(uuid); return guid.ToString();
        }

        public string GetClientId(){ return _clientIdentity != null ? _clientIdentity.ClientId : null; }
        public string[] GetClientGroups(){ return _clientIdentity != null && _clientIdentity.Groups != null ? _clientIdentity.Groups : new string[0]; }
        public string[] GetClientTags(){ return _clientIdentity != null && _clientIdentity.Tags != null ? _clientIdentity.Tags : new string[0]; }
        public string GetActiveVersion(){ return _config.ActiveVersion; }
        public void SetActiveVersion(string version){ _config.ActiveVersion = version; SaveConfig(); }


        public void CleanupStagingVersionFolders()
        {
            try
            {
                if (!Directory.Exists(_versionsRoot)) return;
                var dirs = Directory.GetDirectories(_versionsRoot);
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    // Xóa các thư mục staging tạm (ví dụ tạo bởi ZIP) còn sót lại do update bị dừng giữa chừng.
                    if (!string.IsNullOrEmpty(name) &&
                        name.EndsWith(".__zipstaging", StringComparison.OrdinalIgnoreCase))
                    {
                        try { Directory.Delete(dir, true); }
                        catch { }
                    }
                }
            }
            catch
            {
                // Không để lỗi cleanup staging làm crash app.
            }
        }

        public string[] GetInstalledVersions()
        {
            if (!Directory.Exists(_versionsRoot)) return new string[0];
            return Directory.GetDirectories(_versionsRoot).Select(Path.GetFileName).Where(n=>!string.IsNullOrWhiteSpace(n)).OrderBy(v=>v).ToArray();
        }
        public string GetVersionFolder(string version){ return Path.Combine(_versionsRoot, version); }

        internal LauncherConfig GetConfigSnapshot(){ var json = JsonConvert.SerializeObject(_config); return JsonConvert.DeserializeObject<LauncherConfig>(json); }
        internal UpdateState GetUpdateStateSnapshot(){ var json = JsonConvert.SerializeObject(_updateState); return JsonConvert.DeserializeObject<UpdateState>(json); }

        public void CleanupOldVersions(int keepVersions)
        {
            try
            {
                if (!Directory.Exists(_versionsRoot)) return;
                var all = Directory.GetDirectories(_versionsRoot).Select(Path.GetFileName).Where(n=>!string.IsNullOrWhiteSpace(n)).OrderByDescending(v=>v).ToList();
                if (all.Count <= keepVersions) return;
                var active = _config.ActiveVersion;
                var toKeep = new System.Collections.Generic.HashSet<string>(all.Take(keepVersions), StringComparer.OrdinalIgnoreCase);
                foreach (var v in all){ if (string.Equals(v, active, StringComparison.OrdinalIgnoreCase)) continue; if (!toKeep.Contains(v)){ var dir = Path.Combine(_versionsRoot, v); try{ Directory.Delete(dir,true);}catch{} } }
            } catch {}
        }

        public void EnsureActiveVersionFromSingleInstalled()
        {
            if (!string.IsNullOrWhiteSpace(_config.ActiveVersion)) return;
            var versions = GetInstalledVersions();
            if (versions.Length == 1){ _config.ActiveVersion = versions[0]; SaveConfig(); }
        }

        internal bool CanAttemptUpdateVersion(string version, int maxAttempts, int retryMinutes)
        {
            if (string.IsNullOrWhiteSpace(version)) return false;
            var info = _updateState.GetOrCreateVersionInfo(version); var now = DateTime.UtcNow;
            if (maxAttempts <= 0) return true;
            if (info.AttemptCount < maxAttempts) return true;
            if (retryMinutes <= 0) return false;
            if (now >= info.LastAttemptUtc.AddMinutes(retryMinutes)){ info.AttemptCount = 0; SaveUpdateState(); return true; }
            return false;
        }

        internal void RegisterUpdateAttempt(string version, bool success, string errorKind)
        {
            if (string.IsNullOrWhiteSpace(version)) return;
            var info = _updateState.GetOrCreateVersionInfo(version); info.LastAttemptUtc = DateTime.UtcNow;
            if (success){ info.LastError = null; info.AttemptCount = 0; _updateState.LastSuccessVersion = version; }
            else { info.AttemptCount++; info.LastError = errorKind; }
            SaveUpdateState();
        }
    }
}
