using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dh.AppLauncher
{
    public static class FileHelper
    {
        public static string ComputeFileMd5(string path)
        {
            if (!File.Exists(path)) return null;
            using (var md5 = MD5.Create())
            using (var s = File.OpenRead(path))
            {
                var hash = md5.ComputeHash(s);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static string QuotePath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "\"\"";
            p = p.Trim(); if (p.StartsWith("\"") && p.EndsWith("\"")) return p; return "\"" + p + "\"";
        }
    }
}
