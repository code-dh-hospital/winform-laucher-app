using Dh.AppLauncher.CoreEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dh.Updater.DebugConsole.Core
{
    class ProgramMain
    {
        public static int Run(string[] args)
        {
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version != null ? asm.GetName().Version.ToString() : "(no version)";
            var env = AppEnvironment.Initialize(typeof(ProgramMain).Namespace);
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
