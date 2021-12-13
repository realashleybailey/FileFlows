namespace FileFlows.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text.Json.Serialization;

    public class PluginInfo : FileFlowObject
    {
        public bool Enabled { get; set; }
        public string Assembly { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
        public bool HasSettings { get; set; }
        public ExpandoObject Settings { get; set; }

        public List<ElementField> Fields { get; set; }
    }

    public class PluginInfoModel : PluginInfo
    {
        public string LatestVersion { get; set; }

        public bool UpdateAvailable
        {
            get
            {
                System.Version latest, current;
                if (string.IsNullOrEmpty(LatestVersion) || System.Version.TryParse(LatestVersion, out latest) == false)
                    return false;
                if (string.IsNullOrEmpty(Version) || System.Version.TryParse(Version, out current) == false)
                    return false;
                return current < latest;
            }
        }
    }
}