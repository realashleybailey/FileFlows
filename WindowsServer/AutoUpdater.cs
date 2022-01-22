using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.WindowsServer
{
    internal class AutoUpdater
    {
        private static string UpdateDirectory;
        private static string WindowsServerExe;
        private static bool InitDone = false;
        public static void Init()
        {
            if (InitDone)
                return;
            InitDone = true;

            UpdateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "updates");
            if(Directory.Exists(UpdateDirectory) == false)
                Directory.CreateDirectory(UpdateDirectory);

            WindowsServerExe = Assembly.GetExecutingAssembly()?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "FileFlows.exe");

            FileSystemWatcher watcher = new FileSystemWatcher(UpdateDirectory);
            watcher.NotifyFilter =
                             NotifyFilters.CreationTime |
                             NotifyFilters.DirectoryName |
                             NotifyFilters.FileName |
                             NotifyFilters.LastWrite |
                             NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }
        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            CheckForUpdate();
        }

        private static void CheckForUpdate()
        {
            var update = GetUpdate();
            if (string.IsNullOrEmpty(update.Item1))
                return;

            RunUpdate(update.Item1, update.Item2);
        }

        private static (string, string) GetUpdate()
        {
            var rgxVersion = new Regex(@"(?<=(^FileFlows-))([\d]+\.){3}[\d]+(?=(\.msi$))");
            var currentVersion = Version.Parse(Globals.Version);
            foreach(var file in new DirectoryInfo(UpdateDirectory).GetFiles("*.msi"))
            {
                var match = rgxVersion.Match(file.Name);
                if(match.Success == false)
                    continue;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                Version version;
                if (Version.TryParse(match.Value, out version) == false)
                    continue;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                if (version > currentVersion)
                    return (file.FullName, match.Value);
            }
            return (string.Empty, string.Empty);
        }

        public static void RunUpdate(string msi, string version)
        {
            Console.WriteLine($"Running update [{version}: {msi}");
            string tempFile = Path.Combine(Path.GetTempPath(), $"FileFlowsUpdate_{version}.bat");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("timeout /t 5 /nobreak");
            sb.AppendLine($"msiexec /i \"{msi}\" /quiet /qn");
            sb.AppendLine("timeout /t 5 /nobreak");
            sb.AppendLine($"del \"{tempFile}\"");
            sb.AppendLine(WindowsServerExe);
            sb.AppendLine($"del \"{msi}\"");
            sb.AppendLine(WindowsServerExe);
            File.WriteAllText(tempFile, sb.ToString());
            Console.WriteLine("Starting bat file update: " + tempFile);
            Process.Start(tempFile);
            Form1.Instance?.QuitMe();
        }
    }
}
