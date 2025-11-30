using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dh.AppLauncher.Manifest
{
    internal sealed class ManifestFileEntryRaw
    {
        [JsonProperty("md5")]  public string Md5 { get; set; }
        [JsonProperty("url")]  public string Url { get; set; }
        [JsonProperty("urls")] public string[] Urls { get; set; }
    }

    internal sealed class ClientMatchRuleRaw
    {
        [JsonProperty("client_ids")] public string[] ClientIds { get; set; }
        [JsonProperty("machine_names")] public string[] MachineNames { get; set; }
        [JsonProperty("os_versions")] public string[] OsVersions { get; set; }
        [JsonProperty("groups")] public string[] Groups { get; set; }
        [JsonProperty("tags")] public string[] Tags { get; set; }
    }

    internal sealed class ClientMatchConfigRaw
    {
        [JsonProperty("rules")] public ClientMatchRuleRaw[] Rules { get; set; }
        [JsonProperty("required")] public bool Required { get; set; }
    }

    internal sealed class ManifestRaw
    {
        [JsonProperty("version")]      public string Version { get; set; }
        [JsonProperty("sha512")]       public string Sha512 { get; set; }
        [JsonProperty("package_type")] public string PackageType { get; set; }
        [JsonProperty("urls")]         public string[] Urls { get; set; }
        [JsonProperty("zip_urls")]      public string[] ZipUrls { get; set; }
        [JsonProperty("file_md5")]     public Dictionary<string, ManifestFileEntryRaw> FileMd5 { get; set; }
        [JsonProperty("files")]        public Dictionary<string, ManifestFileEntryRaw> Files { get; set; }
        [JsonProperty("changelog")]    public string Changelog { get; set; }
        [JsonProperty("update_level")] public string UpdateLevel { get; set; }
        [JsonProperty("client_match")] public ClientMatchConfigRaw ClientMatch { get; set; }
    }

    public sealed class UpdateFileEntry
    {
        public string FileName { get; private set; }
        public string Md5 { get; private set; }
        public string Url { get; private set; }
        public string[] Urls { get; private set; }
        public UpdateFileEntry(string fileName, string md5, string url, string[] urls){ FileName=fileName; Md5=md5; Url=url; Urls=urls ?? new string[0]; }
    }

    public sealed class UpdateManifest
    {
        public string Version { get; private set; }
        public string Sha512 { get; private set; }
        public string PackageType { get; private set; }
        public string[] Urls { get; private set; }
        public string[] ZipUrls { get; private set; }
        public string Changelog { get; private set; }
        public IReadOnlyDictionary<string, UpdateFileEntry> Files { get; private set; }
        public string UpdateLevel { get; private set; }
        internal ClientMatchConfigRaw ClientMatch { get; private set; }

        private UpdateManifest(string version, string sha512, string packageType, string[] urls, string[] zipUrls, string changelog, string updateLevel, ClientMatchConfigRaw match, Dictionary<string, UpdateFileEntry> files)
        { Version=version; Sha512=sha512; PackageType=packageType; Urls=urls ?? new string[0]; Changelog=changelog; Files=files; UpdateLevel=updateLevel; ClientMatch=match; }

        public static UpdateManifest FromJson(string json)
        {
            var raw = JsonConvert.DeserializeObject<ManifestRaw>(json) ?? new ManifestRaw();
            var dict = new Dictionary<string, UpdateFileEntry>(StringComparer.OrdinalIgnoreCase);
            var src = (raw.FileMd5 != null && raw.FileMd5.Count > 0) ? raw.FileMd5 : raw.Files;
            if (src != null)
            {
                foreach (var kv in src)
                {
                    var e = kv.Value; if (e == null) continue;
                    dict[kv.Key] = new UpdateFileEntry(kv.Key, e.Md5 ?? string.Empty, e.Url ?? string.Empty, e.Urls ?? new string[0]);
                }
            }
            return new UpdateManifest(raw.Version, raw.Sha512, raw.PackageType, raw.Urls, raw.ZipUrls, raw.Changelog, raw.UpdateLevel, raw.ClientMatch, dict);
        }

        public static UpdateManifest FromXml(string xml)
        {
            var doc = new XmlDocument(); doc.LoadXml(xml);
            var json = JsonConvert.SerializeXmlNode(doc, Formatting.None, true);
            var jObj = JObject.Parse(json);
            throw new NotImplementedException("XML mapping not implemented.");
        }
    }
}
