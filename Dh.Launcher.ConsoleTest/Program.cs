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

                int rc = BaseLauncher.Run(opt);
                Console.WriteLine("ExitCode: {0}", rc);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return rc;
            }
        }
    }
}
