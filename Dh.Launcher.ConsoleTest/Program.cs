// Program.cs - Dh.Launcher.ConsoleTest
// Chỉnh sửa: 2025-11-30 - ChatGPT (assistant)
// Lý do: 
//  - Fix lỗi namespace/event: SummaryChangedFilesAvailable là event của class AppLauncher,
//    không phải namespace Dh.AppLauncher.
//  - Bỏ sử dụng AppLaunchOptions.ManifestUrls (đọc manifest từ launcher.json).
//  - Đồng bộ với kiến trúc: launcher load DLL core bằng reflection trong cùng process.

using System;
using Dh.AppLauncher;
using Dh.AppLauncher.Core;
using Dh.AppLauncher.Update;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Launcher.ConsoleTest
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Đảm bảo chỉ một instance sample launcher chạy để tránh 2 process
            // cùng lúc ghi vào cùng LocalRoot.
            using (var guard = new LauncherInstanceGuard("Dh.Updater.SampleApp"))
            {
                if (!guard.HasHandle)
                {
                    Console.WriteLine("Another instance of Dh.Updater.SampleApp launcher is already running. Exiting.");
                    return 0;
                }

                var env = AppEnvironment.Initialize("Dh.Updater.SampleApp");
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
                        "[UpdateProgress] stage={0}, downloaded={1}, total={2}",
                        e.Stage,
                        e.BytesDownloaded,
                        e.TotalBytes);
                };

                BaseLauncher.UpdateCompleted += (s, e) =>
                {
                    Console.WriteLine(
                        "[UpdateCompleted] success={0}, newVersion={1}, message={2}",
                        e.Success,
                        e.NewVersion,
                        e.Message ?? "(null)");
                };

                BaseLauncher.SummaryChangedFilesAvailable += (s, e) =>
                {
                    Console.WriteLine("SummaryChangedFiles for version {0} (dryRun={1})",
                        e.Version,
                        e.DryRun);

                    Console.WriteLine("  Changed files:");
                    foreach (var f in e.ChangedFiles ?? Array.Empty<string>())
                    {
                        Console.WriteLine("    " + f);
                    }

                    Console.WriteLine(
                        "  PlannedDownloadBytes={0}, KnownChangedBytes={1}",
                        e.PlannedDownloadBytes,
                        e.KnownChangedBytes);
                };

                // ========== Cấu hình AppLaunchOptions ==========

                var opt = new AppLaunchOptions
                {
                    AppName = "Dh.Updater.SampleApp",

                    // Tên assembly core của console sample:
                    CoreAssemblyName = "Dh.Updater.DebugConsole.Core.dll",
                    CoreEntryType = "Dh.Updater.DebugConsole.Core.Program",
                    CoreEntryMethod = "Main",

                    Args = args,
                    AutoCheckUpdates = true,
                    UpdateCheckMode = UpdateCheckMode.OnStartup,
                    KeepVersions = 5,
                    DryRunUpdate = false
                };

                // Lưu ý:
                // - URL manifest được lấy từ launcher.json trong LocalRoot,
                //   không set qua opt.ManifestUrls nữa.

                int rc = AppLauncher.Run(opt);
                Console.WriteLine("ExitCode: {0}", rc);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return rc;
            }
        }
    }
}
