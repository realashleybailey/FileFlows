namespace FileFlows.Shared.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using FileFlows.Plugin.Attributes;

    public class Settings : FileFlowObject
    {
        /// <summary>
        /// Gets or sets if plugins should automatically be updated when new version are available online
        /// </summary>
        public bool AutoUpdatePlugins { get; set; }

        /// <summary>
        /// Gets or sets if the server should automatically update when a new version is available online
        /// </summary>
        public bool AutoUpdate { get; set; }

        /// <summary>
        /// Gets or sets if nodes should be automatically updated when the server is updated
        /// </summary>
        public bool AutoUpdateNodes { get; set; }

        /// <summary>
        /// Gets or sets if telemetry should be disabled
        /// </summary>
        public bool DisableTelemetry { get; set; }
        
        /// <summary>
        /// Gets or sets if the library file logs should be saved in a compressed format to reduce file size
        /// </summary>

        public bool CompressLibraryFileLogs { get; set; }

        private List<string> _PluginRepositoryUrls = new ();
        /// <summary>
        /// Gets or sets a list of available URLs to additional plugin repositories
        /// </summary>
        public List<string> PluginRepositoryUrls
        {
            get => _PluginRepositoryUrls;
            set => _PluginRepositoryUrls = value ?? new();
        }

        /// <summary>
        /// Gets or sets if this is running on Windows
        /// </summary>
        public bool IsWindows { get; set; }
        
        /// <summary>
        /// Gets or sets if this is running inside Docker
        /// </summary>
        public bool IsDocker { get; set; }
        
        /// <summary>
        /// Gets or sets the FileFlows version number
        /// </summary>
        public string Version { get; set; }
    }

}