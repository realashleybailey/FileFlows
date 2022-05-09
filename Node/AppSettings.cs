using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.Node
{
    public class AppSettings
    {
        public static string? ForcedServerUrl { get; set; }
        public static string? ForcedTempPath { get; set; }
        public static string? ForcedHostName { get; set; }

        private string _ServerUrl = string.Empty;
        private string _TempPath = string.Empty;
        public string ServerUrl
        {
            get
            {
                if (string.IsNullOrEmpty(ForcedServerUrl) == false)
                    return ForcedServerUrl;
                return _ServerUrl;
            }
            set
            {
                _ServerUrl = value ?? String.Empty;
            }
        }

        public string TempPath
        {
            get
            {
                if (string.IsNullOrEmpty(ForcedTempPath) == false)
                    return ForcedTempPath;
                return _TempPath;
            }
            set
            {
                _TempPath = value ?? String.Empty;
            }
        }

        public string HostName
        {
            get
            {
                if (string.IsNullOrEmpty(ForcedHostName) == false)
                    return ForcedHostName;
                return Environment.MachineName;
            }
        }

        public int Runners { get; set; }
        public bool Enabled { get; set; }

        public void Save()
        {
            Save(this);
        }

        public static void Init()
        {
            Instance = Load();
        }

        public static AppSettings Instance { get; set; } = new AppSettings();

        public static void Save(AppSettings settings)
        {
            if (settings == null)
                return;
            Instance = settings;
            string json = System.Text.Json.JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(DirectoryHelper.NodeConfigFile, json);
        }
        public static AppSettings Load()
        {
            string file = DirectoryHelper.NodeConfigFile;
            if (File.Exists(file) == false)
            {
                AppSettings settings = new();
                settings.TempPath = Path.Combine(Path.GetTempPath(), "FileFlows");
                settings.Save();
                return settings;
            }
            try
            {
                string json = File.ReadAllText(file);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new ();
            }
            catch (Exception) { }
            return new();
        }

        public static bool IsConfigured()
        {
            return string.IsNullOrWhiteSpace(Load().ServerUrl) == false;
        }
    }
}
