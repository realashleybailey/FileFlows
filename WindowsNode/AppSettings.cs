using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.WindowsNode
{
    internal class AppSettings
    {
        public string ServerUrl { get; set; }
        public string TempPath { get; set; }
        public int Runners { get; set; }
        public bool Enabled { get; set; }

        public void Save()
        {
            Save(this);
        }

        internal static void Init()
        {
            Instance = Load();
        }

        internal static AppSettings Instance { get; set; }

        public static void Save(AppSettings settings)
        {
            if (settings == null)
                return;
            Instance = settings;
            string json = System.Text.Json.JsonSerializer.Serialize(settings);
            File.WriteAllText(GetAppSettingsFile(), json);
        }
        public static AppSettings Load()
        {
            string file = GetAppSettingsFile();
            if (File.Exists(file) == false)
                return new ();
            try
            {
                string json = File.ReadAllText(file);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new ();
            }
            catch (Exception) { }
            return new();
        }

        public static bool IsConfigured()
        {
            return string.IsNullOrWhiteSpace(Load().ServerUrl) == false;
        }

        private static string GetAppSettingsFile()
        {
            return Path.Combine(GetPath(), "fileflows.config");
        }

        private static string GetPath()
        {            
            string exe = Application.ExecutablePath;
            return Path.GetDirectoryName(exe);
        }
    }
}
