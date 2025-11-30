using System;
using Dh.AppLauncher;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Launcher.ConsoleTest
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Đảm bảo chỉ một instance sample launcher chạy để tránh 2 process cùng lúc ghi vào cùng LocalRoot.
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

                Dh.AppLauncher.AppLauncher.SummaryChangedFilesAvailable += (s, e) =>
                {
                    Console.WriteLine("SummaryChangedFiles for version {0} (dryRun={1})", e.Version, e.DryRun);
                    Console.WriteLine("  Changed files:");
                    foreach (var f in e.ChangedFiles ?? new string[0]) Console.WriteLine("    " + f);
                    Console.WriteLine("  PlannedDownloadBytes={0}, KnownChangedBytes={1}", e.PlannedDownloadBytes, e.KnownChangedBytes);
                };

                var opt = new AppLaunchOptions();
                opt.AppName = "Dh.Updater.SampleApp";
                opt.ManifestUrls = new[]
                {
                    "http://localhost:3000/manifest/v1/latest.json"
                };
                opt.Args = args;
                opt.AutoCheckUpdates = true;
                opt.UpdateCheckMode = UpdateCheckMode.OnStartup;
                opt.KeepVersions = 5;
                opt.DryRunUpdate = false;

                int rc = AppLauncher.Run(opt);
                Console.WriteLine("ExitCode: {0}", rc);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return rc;
            }
        }
    }
}
