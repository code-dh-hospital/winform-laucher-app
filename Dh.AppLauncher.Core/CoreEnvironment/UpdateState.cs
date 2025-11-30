using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dh.AppLauncher.CoreEnvironment
{
    internal sealed class FailedVersionInfo
    {
        [JsonProperty("attempt_count")] public int AttemptCount { get; set; }
        [JsonProperty("last_attempt_utc")] public DateTime LastAttemptUtc { get; set; }
        [JsonProperty("last_error")] public string LastError { get; set; }
        public FailedVersionInfo(){ AttemptCount=0; LastAttemptUtc=DateTime.MinValue; LastError=null; }
    }

    internal sealed class UpdateState
    {
        [JsonProperty("last_success_version")] public string LastSuccessVersion { get; set; }
        [JsonProperty("failed_versions")] public Dictionary<string, FailedVersionInfo> FailedVersions { get; set; }
        public UpdateState(){ FailedVersions = new Dictionary<string, FailedVersionInfo>(StringComparer.OrdinalIgnoreCase); }
        public FailedVersionInfo GetOrCreateVersionInfo(string version)
        {
            FailedVersionInfo info;
            if (!FailedVersions.TryGetValue(version, out info)){ info = new FailedVersionInfo(); FailedVersions[version] = info; }
            return info;
        }
    }
}
