using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileFlows.Node
{
    public class AppSettings
    {
        public string ServerUrl { get; set; } = String.Empty;
        public string TempPath { get; set; } = String.Empty;
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

        public static AppSettings Instance { get; set; }

        public static void Save(AppSettings settings)
        {
            if (settings == null)
                return;
            Instance = settings;
            string json = System.Text.Json.JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(GetAppSettingsFile(), json);
        }
        public static AppSettings Load()
        {
            string file = GetAppSettingsFile();
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
            string dll = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(dll);
        }
    }
}
