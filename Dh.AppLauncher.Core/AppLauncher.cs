using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using Dh.AppLauncher.CoreEnvironment;
using Dh.AppLauncher.Logging;
using Dh.AppLauncher.Update;
using Dh.AppLauncher.Manifest;
using Dh.AppLauncher.Http;

namespace Dh.AppLauncher.Core
{
    public sealed class UpdateEnforcementInfoEventArgs : EventArgs
    {
        public string CurrentVersion { get; private set; }
        public string NewVersion { get; private set; }
        public UpdateEnforcementLevel Level { get; private set; }
        public bool UpdateApplied { get; private set; }
        public UpdateEnforcementInfoEventArgs(string cur, string neu, UpdateEnforcementLevel level, bool applied){ CurrentVersion=cur; NewVersion=neu; Level=level; UpdateApplied=applied; }
    }

    public static class BaseLauncher
    {
        public static event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;
        public static event EventHandler<UpdateProgressEventArgs> UpdateProgress;
        public static event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;
        public static event EventHandler<UpdateSummaryEventArgs> UpdateSummaryAvailable;
        public static event EventHandler<ChangedFilesSummaryEventArgs> ChangedFilesSummaryAvailable;
        public static event EventHandler<SummaryChangedFilesEventArgs> SummaryChangedFilesAvailable;
        public static event EventHandler<UpdateEnforcementInfoEventArgs> UpdateEnforcementEvaluated;
        public static Func<UpdateEnforcementInfoEventArgs, bool> ConfirmUpdateHandler;

        public static int Run(AppLaunchOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            UpdateBackoffTimer timer = null;
            try
            {
                var env = AppEnvironment.Initialize(options.AppName);
                LogManager.Initialize(env);
                env.EnsureActiveVersionFromSingleInstalled();
                var configSnapshot = env.GetConfigSnapshot();

                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    try
                    {
                        var name = new AssemblyName(e.Name).Name + ".dll";
                        var activeVersion = env.GetActiveVersion();
                        if (!string.IsNullOrWhiteSpace(activeVersion))
                        {
                            var activeFolder = env.GetVersionFolder(activeVersion);
                            var candidate = Path.Combine(activeFolder, name);
                            if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);

                            var versions = env.GetInstalledVersions();
                            foreach (var v in versions.Reverse())
                            {
                                if (string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase)) continue;
                                var p = Path.Combine(env.GetVersionFolder(v), name);
                                if (File.Exists(p))
                                {
                                    try { Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(activeFolder,name))); File.Copy(p, Path.Combine(activeFolder,name), true); } catch {}
                                    return Assembly.LoadFrom(p);
                                }
                            }

                            try
                            {
                                var manifestPath = env.GetVersionManifestPath(activeVersion);
                                if (File.Exists(manifestPath))
                                {
                                    var text = File.ReadAllText(manifestPath);
                                    var m = UpdateManifest.FromJson(text);
                                    if (m!=null && m.Files!=null)
                                    {
                                        UpdateFileEntry entry;
                                        if (m.Files.TryGetValue(name, out entry))
                                        {
                                            var urls = (entry.Urls!=null && entry.Urls.Length>0) ? entry.Urls : (!string.IsNullOrWhiteSpace(entry.Url) ? new string[]{entry.Url}: new string[0]);
                                            foreach (var u in urls)
                                            {
                                                try
                                                {
                                                    var bytes = Http.HttpClientFactory.Client.GetByteArrayAsync(u).GetAwaiter().GetResult();
                                                    Directory.CreateDirectory(activeFolder); var target = Path.Combine(activeFolder, name); File.WriteAllBytes(target, bytes);
                                                    var expected = (entry.Md5??string.Empty).ToLowerInvariant();
                                                    if (!string.IsNullOrWhiteSpace(expected))
                                                    {
                                                        var md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(target);
                                                        if (!string.Equals(md5, expected, StringComparison.OrdinalIgnoreCase)) { try{File.Delete(target);}catch{} continue; }
                                                    }
                                                    return Assembly.LoadFrom(target);
                                                } catch {}
                                            }
                                        }
                                    }
                                }
                            } catch {}

                            if (configSnapshot.VersionManifestTemplates!=null && configSnapshot.VersionManifestTemplates.Length>0)
                            {
                                foreach (var t in configSnapshot.VersionManifestTemplates)
                                {
                                    if (string.IsNullOrWhiteSpace(t)) continue;
                                    var url = t.Replace("{version}", activeVersion);
                                    try
                                    {
                                        var resp = Http.HttpClientFactory.Client.GetAsync(url).GetAwaiter().GetResult();
                                        if (!resp.IsSuccessStatusCode) continue;
                                        var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                        var m = UpdateManifest.FromJson(text);
                                        if (m==null || m.Files==null) continue;
                                        UpdateFileEntry entry;
                                        if (m.Files.TryGetValue(name, out entry))
                                        {
                                            var urls = (entry.Urls!=null && entry.Urls.Length>0) ? entry.Urls : (!string.IsNullOrWhiteSpace(entry.Url) ? new string[]{entry.Url}: new string[0]);
                                            foreach (var u in urls)
                                            {
                                                try
                                                {
                                                    var bytes = Http.HttpClientFactory.Client.GetByteArrayAsync(u).GetAwaiter().GetResult();
                                                    Directory.CreateDirectory(activeFolder); var target = Path.Combine(activeFolder, name); File.WriteAllBytes(target, bytes);
                                                    var expected = (entry.Md5??string.Empty).ToLowerInvariant();
                                                    if (!string.IsNullOrWhiteSpace(expected))
                                                    {
                                                        var md5 = Dh.AppLauncher.FileHelper.ComputeFileMd5(target);
                                                        if (!string.Equals(md5, expected, StringComparison.OrdinalIgnoreCase)) { try{File.Delete(target);}catch{} continue; }
                                                    }
                                                    try { env.SaveVersionManifest(activeVersion, text); } catch {}
                                                    return Assembly.LoadFrom(target);
                                                } catch {}
                                            }
                                        }
                                    } catch {}
                                }
                            }
                        }
                    }
                    catch (Exception ex){ LogManager.Error("AssemblyResolve handler error.", ex); }
                    return null;
                };

                if (options.AutoCheckUpdates && configSnapshot.AutoCheckUpdates)
                {
                    var optPlan = new Update.UpdateOptions();
                    optPlan.ManifestUrls = configSnapshot.LatestManifestUrls;
                    optPlan.KeepVersions = options.KeepVersions > 0 ? options.KeepVersions : configSnapshot.KeepVersions;
                    optPlan.MaxParallelDownloads = 3;
                    optPlan.DryRunOnly = true; // phase 1: plan only
                    optPlan.MaxUpdateAttemptsPerVersion = configSnapshot.MaxUpdateAttemptsPerVersion;
                    optPlan.FailedVersionRetryMinutes = configSnapshot.FailedVersionRetryMinutes;
                    optPlan.AllowDowngrade = configSnapshot.AllowDowngrade;
                    optPlan.DefaultUpdateLevel = ParseUpdateLevel(configSnapshot.DefaultUpdateLevel);

                    var updaterPlan = new Update.UpdateManager(env, optPlan);
                    updaterPlan.UpdateAvailable += (s,e)=> UpdateAvailable?.Invoke(s,e);
                    updaterPlan.UpdateProgress += (s,e)=> UpdateProgress?.Invoke(s,e);
                    updaterPlan.UpdateCompleted += (s,e)=> UpdateCompleted?.Invoke(s,e);
                    updaterPlan.UpdateSummaryAvailable += (s,e)=> UpdateSummaryAvailable?.Invoke(s,e);
                    updaterPlan.ChangedFilesSummaryAvailable += (s,e)=> ChangedFilesSummaryAvailable?.Invoke(s,e);
                    updaterPlan.SummaryChangedFilesAvailable += (s,e)=> SummaryChangedFilesAvailable?.Invoke(s,e);

                    var planResult = updaterPlan.CheckAndUpdateAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var planInfo = new UpdateEnforcementInfoEventArgs(planResult.CurrentVersion, planResult.NewVersion, planResult.EnforcementLevel, planResult.UpdateApplied);
                    var hPlan = UpdateEnforcementEvaluated; if (hPlan != null) hPlan(null, planInfo);

                    bool shouldApply = true;
                    if (planResult.EnforcementLevel == UpdateEnforcementLevel.AskUser && ConfirmUpdateHandler != null)
                    {
                        try { shouldApply = ConfirmUpdateHandler(planInfo); } catch { shouldApply = false; }
                    }

                    if (planResult.EnforcementLevel == UpdateEnforcementLevel.Force)
                        shouldApply = true; // force luôn apply

                    if (shouldApply && planResult.CheckSucceeded && !string.IsNullOrWhiteSpace(planResult.NewVersion))
                    {
                        var optApply = new Update.UpdateOptions();
                        optApply.ManifestUrls = configSnapshot.LatestManifestUrls;
                        optApply.KeepVersions = options.KeepVersions > 0 ? options.KeepVersions : configSnapshot.KeepVersions;
                        optApply.MaxParallelDownloads = 3;
                        optApply.DryRunOnly = options.DryRunUpdate; // nếu app đang yêu cầu dry-run, vẫn không apply
                        optApply.MaxUpdateAttemptsPerVersion = configSnapshot.MaxUpdateAttemptsPerVersion;
                        optApply.FailedVersionRetryMinutes = configSnapshot.FailedVersionRetryMinutes;
                        optApply.AllowDowngrade = configSnapshot.AllowDowngrade;
                        optApply.DefaultUpdateLevel = ParseUpdateLevel(configSnapshot.DefaultUpdateLevel);

                        var updater = new Update.UpdateManager(env, optApply);
                        updater.UpdateAvailable += (s,e)=> UpdateAvailable?.Invoke(s,e);
                        updater.UpdateProgress += (s,e)=> UpdateProgress?.Invoke(s,e);
                        updater.UpdateCompleted += (s,e)=> UpdateCompleted?.Invoke(s,e);
                        updater.UpdateSummaryAvailable += (s,e)=> UpdateSummaryAvailable?.Invoke(s,e);
                        updater.ChangedFilesSummaryAvailable += (s,e)=> ChangedFilesSummaryAvailable?.Invoke(s,e);
                        updater.SummaryChangedFilesAvailable += (s,e)=> SummaryChangedFilesAvailable?.Invoke(s,e);

                        var applyResult = updater.CheckAndUpdateAsync(CancellationToken.None).GetAwaiter().GetResult();
                        var applyInfo = new UpdateEnforcementInfoEventArgs(applyResult.CurrentVersion, applyResult.NewVersion, applyResult.EnforcementLevel, applyResult.UpdateApplied);
                        var hApply = UpdateEnforcementEvaluated; if (hApply != null) hApply(null, applyInfo);

                        if (applyResult.EnforcementLevel == UpdateEnforcementLevel.Force)
                        {
                            var activeNow = env.GetActiveVersion();
                            if (!string.IsNullOrWhiteSpace(applyResult.NewVersion) &&
                                !string.IsNullOrWhiteSpace(activeNow) &&
                                !string.Equals(applyResult.NewVersion, activeNow, StringComparison.OrdinalIgnoreCase))
                            {
                                LogManager.Error("Forced update required to version " + applyResult.NewVersion + " but active version is " + activeNow + ". Abort launch.");
                                return -1000;
                            }
                        }
                    }
                    else if (!shouldApply && planResult.EnforcementLevel == UpdateEnforcementLevel.Force)
                    {
                        // Nếu là force mà user/handler không cho apply -> vẫn không cho chạy
                        LogManager.Error("Forced update required but ConfirmUpdateHandler declined. Abort launch.");
                        return -1000;
                    }

                    if (options.UpdateCheckMode == UpdateCheckMode.OnStartupAndTimer)
                    {
                        var optTimer = new Update.UpdateOptions();
                        optTimer.ManifestUrls = configSnapshot.LatestManifestUrls;
                        optTimer.KeepVersions = options.KeepVersions > 0 ? options.KeepVersions : configSnapshot.KeepVersions;
                        optTimer.MaxParallelDownloads = 3;
                        optTimer.DryRunOnly = false;
                        optTimer.MaxUpdateAttemptsPerVersion = configSnapshot.MaxUpdateAttemptsPerVersion;
                        optTimer.FailedVersionRetryMinutes = configSnapshot.FailedVersionRetryMinutes;
                        optTimer.AllowDowngrade = configSnapshot.AllowDowngrade;
                        optTimer.DefaultUpdateLevel = ParseUpdateLevel(configSnapshot.DefaultUpdateLevel);

                        timer = new UpdateBackoffTimer(env, optTimer, configSnapshot.CheckIntervalMinutes);
                        timer.UpdateSummaryAvailable += (s,e)=> UpdateSummaryAvailable?.Invoke(s,e);
                        timer.ChangedFilesSummaryAvailable += (s,e)=> ChangedFilesSummaryAvailable?.Invoke(s,e);
                        timer.SummaryChangedFilesAvailable += (s,e)=> SummaryChangedFilesAvailable?.Invoke(s,e);
                        timer.UpdateCompleted += (s,e)=> UpdateCompleted?.Invoke(s,e);
                        timer.Start();
                    }
                }

                var active = env.GetActiveVersion();

                if (string.IsNullOrWhiteSpace(active)){ LogManager.Warn("No active version. Stop."); timer?.Dispose(); return -1; }
                var folder = env.GetVersionFolder(active);
                if (!Directory.Exists(folder)){ LogManager.Error("Active folder not found: "+folder); timer?.Dispose(); return -2; }

                var asmPath = Path.Combine(folder, options.CoreAssemblyName);
                if (!System.IO.File.Exists(asmPath)){ LogManager.Error("Core not found: "+asmPath); timer?.Dispose(); return -3; }

                var asm = Assembly.LoadFrom(asmPath);
                var type = asm.GetType(options.CoreEntryType, true, false);
                var method = type.GetMethod(options.CoreEntryMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null){ LogManager.Error("Entry method not found."); timer?.Dispose(); return -4; }

                object result;
                var ps = method.GetParameters();
                if (ps.Length==1 && ps[0].ParameterType==typeof(string[])) result = method.Invoke(null, new object[]{ options.Args ?? new string[0] });
                else if (ps.Length==0) result = method.Invoke(null, null);
                else result = method.Invoke(null, null);

                timer?.Dispose();
                if (method.ReturnType==typeof(int) && result is int) return (int)result;
                return 0;
            }
            catch (Exception ex)
            { try{ LogManager.Error("AppLauncher.Run failed.", ex);}catch{} try{ timer?.Dispose(); }catch{} return -500; }
        }

        private static UpdateEnforcementLevel ParseUpdateLevel(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return UpdateEnforcementLevel.Silent;
            switch (s.Trim().ToLowerInvariant())
            {
                case "silent": return UpdateEnforcementLevel.Silent;
                case "ask": return UpdateEnforcementLevel.AskUser;
                case "force": return UpdateEnforcementLevel.Force;
                default: return UpdateEnforcementLevel.Silent;
            }
        }
    }

    /// <summary>
    /// Helper đơn giản để bảo đảm chỉ một instance launcher chạy cho mỗi appName.
    /// Dùng trong entry point của app (console/winform) để tránh 2 process cùng lúc ghi vào vùng Versions/Config.
    /// </summary>
    public sealed class LauncherInstanceGuard : IDisposable
    {
        private readonly System.Threading.Mutex _mutex;
        private readonly bool _hasHandle;

        /// <summary>
        /// Cho biết mutex có được giữ thành công hay không. Nếu false nghĩa là đã có instance khác đang chạy.
        /// </summary>
        public bool HasHandle { get { return _hasHandle; } }

        public LauncherInstanceGuard(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentException("appName");

            // Đặt tên mutex theo appName, có thể thêm prefix để tránh va chạm với app khác.
            var mutexName = "Global\\DhLauncher_" + appName;
            try
            {
                _mutex = new System.Threading.Mutex(false, mutexName);
                // Thử acquire với timeout 0 để không block nếu đã có process khác.
                _hasHandle = _mutex.WaitOne(0, false);
            }
            catch (System.Threading.AbandonedMutexException)
            {
                // Mutex bị bỏ rơi bởi process trước đó → vẫn coi như mình đã sở hữu để tránh deadlock.
                _hasHandle = true;
            }
            catch
            {
                _hasHandle = false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_mutex != null && _hasHandle)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
            catch
            {
                // Không để lỗi mutex làm crash app khi dispose.
            }
        }
    }


}
