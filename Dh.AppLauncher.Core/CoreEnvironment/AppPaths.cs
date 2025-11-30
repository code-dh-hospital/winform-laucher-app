using System;
using System.IO;

namespace Dh.AppLauncher.Core.CoreEnvironment
{
    /// <summary>
    /// Helper tập trung cho đường dẫn: BinaryRoot (version folder), SharedDataRoot (dùng chung nhiều version).
    /// Mục tiêu: tránh code rải rác Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ...) khắp nơi.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>
        /// Thư mục chứa binary hiện tại (thường là folder phiên bản active).
        /// </summary>
        public static string GetBinaryRoot()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Thư mục Data dùng chung cho nhiều version, nằm trong LocalApplicationData.
        /// </summary>
        public static string GetSharedDataRoot(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentNullException("appName");
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DhLauncherApps",
                appName,
                "Data");
            Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>
        /// Map đường dẫn tương đối (trước đây thường ghi cạnh exe) sang SharedDataRoot.
        /// </summary>
        public static string MapExeRelativeToData(string appName, string relativePath)
        {
            if (relativePath == null) relativePath = string.Empty;
            var dataRoot = GetSharedDataRoot(appName);
            return Path.Combine(dataRoot, relativePath);
        }
    }
}
