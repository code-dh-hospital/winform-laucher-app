using System;
using System.IO;
using Newtonsoft.Json;

namespace Dh.AppLauncher.Core.CoreEnvironment
{
    /// <summary>
    /// MigrationHelper hỗ trợ app cũ chuyển dần IO từ thư mục launcher/legacy sang SharedDataRoot (AppPaths).
    /// - Khi đọc file: ưu tiên DataRoot, nếu chưa có thì tìm ở legacyRoot, copy sang DataRoot, rồi đọc từ DataRoot.
    /// - Khi ghi file: build path trong DataRoot (không ghi cạnh exe nữa).
    /// - Hỗ trợ bootstrap lần đầu: backup thư mục launcher thành version đầu tiên + data chung.
    /// </summary>
    public static class MigrationHelper
    {
        /// <summary>
        /// Đảm bảo một file (relativePath) nằm trong SharedDataRoot.
        /// Nếu chưa có, sẽ tìm trong legacyRoot (mặc định là BinaryRoot) rồi copy sang.
        /// Trả về full path trong SharedDataRoot (có thể file chưa tồn tại nếu không tìm thấy ở legacyRoot).
        /// </summary>
        public static string EnsureOnDataRoot(string appName, string relativePath, string legacyRoot = null)
        {
            if (relativePath == null) relativePath = string.Empty;

            // 1) Path trong DataRoot
            string dataPath = AppPaths.MapExeRelativeToData(appName, relativePath);
            if (File.Exists(dataPath))
                return dataPath;

            // 2) Nếu chưa có trong DataRoot, thử tìm trong legacyRoot
            if (string.IsNullOrWhiteSpace(legacyRoot))
                legacyRoot = AppPaths.GetBinaryRoot();

            if (!string.IsNullOrWhiteSpace(legacyRoot) && Directory.Exists(legacyRoot))
            {
                string legacyPath = Path.Combine(legacyRoot, relativePath);
                if (File.Exists(legacyPath))
                {
                    string dir = Path.GetDirectoryName(dataPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    // Chỉ copy nếu DataRoot chưa có file
                    File.Copy(legacyPath, dataPath, false);
                    return dataPath;
                }
            }

            // 3) Không tìm thấy ở đâu, trả về path trong DataRoot để code phía trên có thể tự tạo mới
            return dataPath;
        }

        /// <summary>
        /// Trả về path để ghi file trong SharedDataRoot (đảm bảo thư mục tồn tại).
        /// Không động tới legacyRoot.
        /// </summary>
        public static string GetDataPathForWrite(string appName, string relativePath)
        {
            if (relativePath == null) relativePath = string.Empty;
            string dataPath = AppPaths.MapExeRelativeToData(appName, relativePath);
            string dir = Path.GetDirectoryName(dataPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dataPath;
        }

        /// <summary>
        /// Thử bootstrap lần đầu từ thư mục legacy (launcher root) sang cấu trúc version + data.
        /// - Nếu đã có active.json thì không làm gì.
        /// - Nếu chưa có:
        ///   + Tạo version đầu tiên (vd: 1.0.0.0) trong Versions.
        ///   + Copy *.exe, *.dll từ legacyRoot vào folder version (giữ nguyên cấu trúc thư mục con).
        ///   + Tất cả file còn lại copy sang SharedDataRoot (AppPaths).
        ///   + Ghi active.json và manifest migrated.
        /// </summary>
        public static void TryInitialBootstrapFromLegacy(AppEnvironment env, string appName, string legacyRoot = null)
        {
            if (env == null) throw new ArgumentNullException("env");
            if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentNullException("appName");

            string localRoot = env.GetLocalRoot();
            string configDir = Path.Combine(localRoot, "Config");
            string versionsDir = Path.Combine(localRoot, "Versions");
            string activePath = Path.Combine(configDir, "active.json");

            // Nếu đã có active.json thì coi như đã bootstrap/migrate rồi
            if (File.Exists(activePath))
                return;

            if (string.IsNullOrWhiteSpace(legacyRoot))
                legacyRoot = AppPaths.GetBinaryRoot();

            if (string.IsNullOrWhiteSpace(legacyRoot) || !Directory.Exists(legacyRoot))
                return;

            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(versionsDir);

            string initialVersion = "1.0.0.0";
            string versionDir = Path.Combine(versionsDir, initialVersion);
            Directory.CreateDirectory(versionDir);

            string dataRoot = AppPaths.GetSharedDataRoot(appName);

            // Duyệt tất cả file trong legacyRoot
            foreach (var file in Directory.GetFiles(legacyRoot, "*", SearchOption.AllDirectories))
            {
                string rel = file.Substring(legacyRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string ext = Path.GetExtension(file);

                // Bỏ qua file active launcher.exe chính nếu muốn (optional)
                // Ở đây chỉ bỏ qua nếu file nằm trong localRoot để tránh vòng lặp copy
                try
                {
                    if (file.StartsWith(localRoot, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                catch { }

                if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Binary copy vào folder version
                    string dest = Path.Combine(versionDir, rel);
                    string destDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (!File.Exists(dest))
                        File.Copy(file, dest, true);
                }
                else
                {
                    // Các file còn lại coi như data chung → copy sang SharedDataRoot
                    string dest = AppPaths.MapExeRelativeToData(appName, rel);
                    string destDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (!File.Exists(dest))
                        File.Copy(file, dest, true);
                }
            }

            // Ghi active.json
            var activeObj = new
            {
                version = initialVersion,
                changed_utc = DateTime.UtcNow.ToString("o")
            };
            Directory.CreateDirectory(configDir);
            File.WriteAllText(activePath, JsonConvert.SerializeObject(activeObj, Formatting.Indented));

            // Ghi manifest migrated cơ bản để đánh dấu version đầu
            string manifestsDir = Path.Combine(configDir, "manifests");
            Directory.CreateDirectory(manifestsDir);
            string manifestPath = Path.Combine(manifestsDir, "manifest-" + initialVersion + "-migrated.json");
            var manifestObj = new
            {
                version = initialVersion,
                sha512 = string.Empty,
                package_type = "migrated_local",
                urls = new string[0],
                changelog = "Initial migrated version from legacy launcher folder.",
                file_md5 = new { }
            };
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifestObj, Formatting.Indented));
        }
    }
}
