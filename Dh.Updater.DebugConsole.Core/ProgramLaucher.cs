using System;
using System.Reflection;
using Dh.AppLauncher;
using Dh.AppLauncher.Core;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Updater.DebugConsole.Core
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Đảm bảo chỉ một instance sample launcher chạy để tránh 2 process
            // cùng lúc ghi vào cùng LocalRoot.
            var mainAppName = typeof(Program).Namespace;
            using (var guard = new LauncherInstanceGuard(mainAppName))
            {
                if (!guard.HasHandle)
                {
                    Console.WriteLine($"Another instance of {mainAppName} launcher is already running. Exiting.");
                    return 0;
                }

                var env = AppEnvironment.Initialize(mainAppName);
                Console.WriteLine("Active: {0}", env.GetActiveVersion() ?? "(null)");
                Console.WriteLine("ClientId: {0}", env.GetClientId() ?? "(null)");
                Console.WriteLine("LocalRoot: {0}", env.LocalRoot);

                // ========== Đăng ký các event update để debug ==========

                BaseLauncher.UpdateAvailable += (s, e) =>
                {
                    Console.WriteLine("[UpdateAvailable] {0} -> {1}", e.CurrentVersion, e.NewVersion);
                };

                BaseLauncher.UpdateProgress += (s, e) =>
                {
                    Console.WriteLine(
                        "[UpdateProgress] file={0}, bytesReceived={1}, totalBytes={2}",
                        e.FileName,
                        e.BytesReceived,
                        e.TotalBytes);
                };

                BaseLauncher.UpdateCompleted += (s, e) =>
                {
                    Console.WriteLine(
                        "[UpdateCompleted] newVersion={0}",
                        e.NewVersion);
                };

                BaseLauncher.SummaryChangedFilesAvailable += (s, e) =>
                {
                    Console.WriteLine("SummaryChangedFiles for version {0} (dryRun={1})",
                        e.NewVersion,
                        e.IsDryRun);

                    Console.WriteLine("  Changed files:");
                    if (e.ChangedFiles != null)
                    {
                        foreach (var f in e.ChangedFiles)
                        {
                            Console.WriteLine("    " + f);
                        }
                    }

                    Console.WriteLine(
                        "  TotalPlannedDownloadBytes={0}, KnownChangedBytes={1}",
                        e.TotalPlannedDownloadBytes,
                        e.KnownChangedBytes);
                };

                // ========== Cấu hình AppLaunchOptions ==========

                var opt = new AppLaunchOptions
                {
                    AppName = mainAppName,

                    // Tên assembly core của console sample:
                    CoreAssemblyName = "Dh.Updater.DebugConsole.Core.exe",
                    CoreEntryType = "Dh.Updater.DebugConsole.Core.ProgramMain",
                    CoreEntryMethod = "Run",

                    Args = args,
                    AutoCheckUpdates = true,
                    UpdateCheckMode = UpdateCheckMode.OnStartup,
                    KeepVersions = 5,
                    DryRunUpdate = false
                };

                // Lưu ý:
                // - URL manifest được lấy từ launcher.json trong LocalRoot,
                //   không set qua opt.ManifestUrls nữa.

                int rc = BaseLauncher.Run(opt);
                Console.WriteLine("ExitCode: {0}", rc);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return rc;

            }
        }
    }
}
