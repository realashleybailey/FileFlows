namespace FileFlows.Shared.Models
{
    using System.Collections.Generic;
    using System.Dynamic;

    public class PluginInfo : FileFlowObject
    {
        public bool Enabled { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }

        public bool HasSettings { get; set; }

        public string Url { get; set; }
        public string Authors { get; set; }
        public string Description { get; set; }
        public string MinimumVersion { get; set; }
        public string PackageName { get; set; }

        //public List<ElementField> Fields { get; set; }

        public List<ElementField> Settings { get; set; }
        
        public List<FlowElement> Elements { get; set; }
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