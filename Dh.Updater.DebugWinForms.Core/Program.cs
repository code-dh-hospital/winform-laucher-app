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

namespace Dh.Launcher.DebugWinForms
{
    internal static class Program
    {
        // WinForms entrypoint => STAThread
        [STAThread]
        private static int Main(string[] args)
        {
            // Đảm bảo chỉ một instance launcher chạy để tránh ghi đè LocalRoot.
            using (var guard = new LauncherInstanceGuard("Dh.Updater.DebugWinForms"))
            {
                if (!guard.HasHandle)
                {
                    MessageBox.Show(
                        "Another instance of Dh.Updater.DebugWinForms is already running.",
                        "Launcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return 0;
                }

                var env = AppEnvironment.Initialize("Dh.Updater.DebugWinForms");
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
                    Dh.AppLauncher.Logging.LogManager.Info(
                        $"[UpdateCompleted] newVersion={e.NewVersion}");
                };

                BaseLauncher.SummaryChangedFilesAvailable += (s, e) =>
                {
                    Dh.AppLauncher.Logging.LogManager.Info(
                        $"SummaryChangedFiles for version {e.NewVersion} (dryRun={e.IsDryRun}) " +
                        $"TotalPlannedDownloadBytes={e.TotalPlannedDownloadBytes}, KnownChangedBytes={e.KnownChangedBytes}");
                };

                // ========== Cấu hình AppLaunchOptions cho WinForms core ==========

                var opt = new AppLaunchOptions
                {
                    // Tên app, dùng để tạo: %LocalAppData%\Dh.Updater.DebugWinForms\
                    AppName = "Dh.Updater.DebugWinForms",

                    // DLL core WinForms:
                    CoreAssemblyName = "Dh.Updater.DebugWinForms.Core.exe",

                    // Host class WinForms đã tạo ở trên:
                    CoreEntryType = "Dh.Updater.DebugWinForms.Core.MainFormHost",

                    // Phương thức entry dùng reflection:
                    CoreEntryMethod = "Run",

                    // Truyền nguyên args vào core => MainFormHost.Run(string[] args)
                    Args = args,

                    AutoCheckUpdates = true,
                    UpdateCheckMode = UpdateCheckMode.OnStartup,
                    KeepVersions = 5,
                    DryRunUpdate = false
                };

                // Lưu ý:
                // - URL manifest vẫn lấy từ launcher.json trong LocalRoot,
                //   không set qua opt.ManifestUrls.

                // Chạy launcher: nó sẽ bootstrap version đầu tiên (nếu cần),
                // tải update (nếu có), sau đó load DLL core và gọi MainFormHost.Run().
                int rc = BaseLauncher.Run(opt);

                // Vì đây là app WinForms, KHÔNG nên Console.ReadKey.
                // Nếu cần thông tin lỗi, dùng log hoặc MessageBox ở chỗ catch trong BaseLauncher.

                return rc;
            }
        }
    }
}
