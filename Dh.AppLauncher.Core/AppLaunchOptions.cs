namespace Dh.AppLauncher
{
    public enum UpdateCheckMode { OnStartup = 0, OnStartupAndTimer = 1, ManualOnly = 2 }
    public enum UpdateEnforcementLevel { Default = 0, Silent = 1, AskUser = 2, Force = 3 }

    public sealed class AppLaunchOptions
    {
        public string AppName { get; set; }
        public string CoreAssemblyName { get; set; }
        public string CoreEntryType { get; set; }
        public string CoreEntryMethod { get; set; }
        public string[] Args { get; set; }
        public bool AutoCheckUpdates { get; set; }
        public UpdateCheckMode UpdateCheckMode { get; set; }
        public int KeepVersions { get; set; }
        public bool DryRunUpdate { get; set; }
        public AppLaunchOptions(){ Args=new string[0]; AutoCheckUpdates=true; UpdateCheckMode=UpdateCheckMode.OnStartup; KeepVersions=5; CoreEntryMethod="Main"; DryRunUpdate=false; }
    }
}
