// Program.cs - Dh.Launcher.DebugWinForms
// Chỉnh sửa: 2025-11-30 - ChatGPT (assistant)
// Lý do: 
//  - Dùng cùng luồng launcher/update như Dh.Launcher.ConsoleTest,
//    nhưng core là WinForms DLL Dh.Updater.DebugWinForms.Core.
//  - Entry của core là MainFormHost.Run (WinForms), không phải Program.Main.
//  - Vẫn có các event update để debug.

// Tham khảo: logic từ Dh.Launcher.ConsoleTest đã gửi.

using System;
using System.Windows.Forms;
using Dh.AppLauncher;
using Dh.AppLauncher.Core;
using Dh.AppLauncher.Update;
using Dh.AppLauncher.CoreEnvironment;
using Dh.AppLauncher.Logging;

namespace Dh.Updater.DebugWinForms
{
    internal static class Program
    {
        // WinForms entrypoint => STAThread
        [STAThread]
        private static int Main(string[] args)
        {
            var typeProgram = typeof(Program);
            var mainAppName = typeProgram.Namespace;
            using (var guard = new LauncherInstanceGuard(mainAppName))
            {
                if (!guard.HasHandle)
                {
                    MessageBox.Show(
                        $"Another instance of {mainAppName} is already running.",
                        "Launcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return 0;
                }
                var env = AppEnvironment.Initialize(mainAppName);
                LogManager.Initialize(env);

                // Bạn có thể log ra file, hoặc dùng Debug.WriteLine.
                // Ở đây demo show vài info bằng Debug/Log chứ không pop-up nhiều.

                // ========== Đăng ký các event update để debug ==========

                BaseLauncher.UpdateAvailable += (s, e) =>
                {
                    // Ví dụ: log ra file qua LogManager thay vì MessageBox để tránh phiền.
                    Dh.AppLauncher.Logging.LogManager.Info(
                    $"[UpdateAvailable] {e.CurrentVersion} -> {e.NewVersion}");
                };

                BaseLauncher.UpdateProgress += (s, e) =>
                {
                    Dh.AppLauncher.Logging.LogManager.Debug(
                        $"[UpdateProgress] file={e.FileName}, bytesReceived={e.BytesReceived}, totalBytes={e.TotalBytes}");
                };

                BaseLauncher.UpdateCompleted += (s, e) =>
                {
                    LogManager.Info(
                        $"[UpdateCompleted] newVersion={e.NewVersion}");
                };

                BaseLauncher.SummaryChangedFilesAvailable += (s, e) =>
                {
                    LogManager.Info(
                        $"SummaryChangedFiles for version {e.NewVersion} (dryRun={e.IsDryRun}) " +
                        $"TotalPlannedDownloadBytes={e.TotalPlannedDownloadBytes}, KnownChangedBytes={e.KnownChangedBytes}");
                };

                // ========== Cấu hình AppLaunchOptions cho WinForms core ==========

                var opt = new AppLaunchOptions
                {
                    // Tên app, dùng để tạo: %LocalAppData%\Dh.Updater.DebugWinForms\
                    AppName = mainAppName,

                    // DLL core WinForms:
                    CoreAssemblyName = "Dh.Updater.DebugWinForms.Core.exe",

                    // Host class WinForms đã tạo ở trên:
                    CoreEntryType = $"{typeof(Updater.DebugWinForms.ProgramMain).Namespace}.{nameof(Updater.DebugWinForms.ProgramMain)}",
                    // Phương thức entry dùng reflection:
                    CoreEntryMethod = nameof(Updater.DebugWinForms.ProgramMain.Run),

                    // Truyền nguyên args vào core => MainFormHost.Run(string[] args)
                    Args = args,

                    AutoCheckUpdates = true,
                    UpdateCheckMode = UpdateCheckMode.OnStartup,
                    KeepVersions = 5,
                    DryRunUpdate = false
                };
                int rc = BaseLauncher.Run(opt);
                return rc;
            }
        }
    }
}
       