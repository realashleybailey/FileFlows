namespace FileFlows.Shared.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using FileFlows.Plugin.Attributes;

    public class Settings : FileFlowObject
    {
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

        /// <summary>
        /// Gets a lof file
        /// </summary>
        /// <param name="logPath">The path where the logs are kept</param>
        /// <param name="uid">the uid of the library file to get a log for</param>
        /// <returns>The log file</returns>
        public string GetLogFile(string logPath, Guid uid)
        {
            if (string.IsNullOrEmpty(logPath))
                return string.Empty;
            
            //if(IsDocker) // docker is in the base directory
                //return System.IO.Path.Combine(logPath, uid + ".log");

            return System.IO.Path.Combine(logPath, "LibraryFiles", uid + ".log");
        }
    }

}