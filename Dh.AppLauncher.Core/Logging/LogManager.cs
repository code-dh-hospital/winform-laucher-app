using System;
using System.IO;
using System.Text;
using Dh.AppLauncher.CoreEnvironment;

namespace Dh.AppLauncher.Logging
{
    public enum LogLevel { Debug, Info, Warning, Error }

    public static class LogManager
    {
        private static AppEnvironment _env; private static readonly object _lockObj = new object(); private static bool _initialized;
        private const long MaxLogSizeBytes = 20L * 1024L * 1024L; // 20MB

        public static void Initialize(AppEnvironment env){ if (env==null) throw new ArgumentNullException("env"); _env=env; _initialized=true; try{ CleanupOldLogs(30);}catch{} Info("LogManager initialized (v6)."); }

        private static string GetLogFilePath(){ var date = DateTime.Now.ToString("yyyy-MM-dd"); return Path.Combine(_env.LogsRoot, "app-" + date + ".log"); }

        private static void CleanupOldLogs(int keepDays)
        {
            if (!Directory.Exists(_env.LogsRoot)) return;
            var cutoff = DateTime.Now.AddDays(-keepDays);
            foreach (var f in Directory.GetFiles(_env.LogsRoot, "app-*.log"))
            { try{ var fi = new FileInfo(f); if (fi.CreationTime < cutoff) fi.Delete(); } catch{} }
        }

        private static void EnsureMaxSize(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                if (fi.Exists && fi.Length > MaxLogSizeBytes)
                {
                    var dir = Path.GetDirectoryName(path);
                    var nameNoExt = Path.GetFileNameWithoutExtension(path);
                    var ext = Path.GetExtension(path);
                    var archiveName = Path.Combine(dir, nameNoExt + "-" + DateTime.Now.ToString("HHmmss") + ".bak" + ext);
                    File.Move(path, archiveName);
                }
            } catch {}
        }

        private static void WriteLog(LogLevel level, string message, Exception ex)
        {
            if (!_initialized) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" [").Append(level.ToString().ToUpperInvariant()).Append("] ").Append(message);
                if (ex != null) { sb.AppendLine().Append(ex.ToString()); }
                var line = sb.ToString();
                lock (_lockObj)
                {
                    var path = GetLogFilePath();
                    EnsureMaxSize(path);
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            } catch {}
        }

        public static void Debug(string message){ WriteLog(LogLevel.Debug, message, null); }
        public static void Info(string message){ WriteLog(LogLevel.Info, message, null); }
        public static void Warn(string message){ WriteLog(LogLevel.Warning, message, null); }
        public static void Error(string message){ WriteLog(LogLevel.Error, message, null); }
        public static void Error(string message, Exception ex){ WriteLog(LogLevel.Error, message, ex); }
    }
}
