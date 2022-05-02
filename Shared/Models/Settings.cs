namespace FileFlows.Shared.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using FileFlows.Plugin.Attributes;

    public class Settings : FileFlowObject
    {

        [Folder(1)]
        [Required]
        public string LoggingPath { get; set; }

        public bool AutoUpdatePlugins { get; set; }

        public bool AutoUpdate { get; set; }

        public bool AutoUpdateNodes { get; set; }

        public bool DisableTelemetry { get; set; }

        private List<string> _PluginRepositoryUrls = new ();
        public List<string> PluginRepositoryUrls
        {
            get => _PluginRepositoryUrls;
            set { _PluginRepositoryUrls = value ?? new(); }
        }

        public bool IsWindows { get; set; }
        public bool IsDocker { get; set; }

        public string Version { get; set; }

        public string GetLogFile(System.Guid uid)
        {
            if (string.IsNullOrEmpty(LoggingPath))
                return string.Empty;
            
            if(IsDocker) // docker is in the base directory
                return System.IO.Path.Combine(LoggingPath, uid + ".log");

            return System.IO.Path.Combine(LoggingPath, "LibraryFiles", uid + ".log");
        }
    }

}