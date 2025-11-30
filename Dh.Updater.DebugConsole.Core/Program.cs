using System;
using System.Reflection;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.Updater.DebugConsole.Core
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version != null ? asm.GetName().Version.ToString() : "(no version)";
            var env = AppEnvironment.Initialize("Dh.Updater.SampleApp");
            Console.WriteLine("=== DebugConsole Core === v{0}", ver);
            Console.WriteLine("Active: {0}", env.GetActiveVersion() ?? "(null)");
            Console.WriteLine("Installed: {0}", string.Join(", ", env.GetInstalledVersions()));
            Console.WriteLine("ClientId: {0}", env.GetClientId() ?? "(null)");
            Console.WriteLine("Press any key to exit core...");
            Console.ReadKey();
            return 0;
        }
    }
}
