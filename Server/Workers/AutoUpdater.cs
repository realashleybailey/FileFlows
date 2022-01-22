namespace FileFlows.Server.Workers
{
    using FileFlows.ServerShared.Workers;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    public class AutoUpdater : Worker
    {
        private static string UpdateDirectory;
        private static string WindowsServerExe;

        public AutoUpdater() : base(ScheduleType.Hourly, 1)
        {
            Logger.Instance.ILog("AutoUpdater: Starting AutoUpdater");
            UpdateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "updates");
            if (Directory.Exists(UpdateDirectory) == false)
            {
                Logger.Instance.ILog("AutoUpdater: Creating updates directory: " + UpdateDirectory);
                Directory.CreateDirectory(UpdateDirectory);
            }
            else
            {
                Logger.Instance.ILog("AutoUpdater: Watch Directory:: " + UpdateDirectory);
            }

            WindowsServerExe = Assembly.GetExecutingAssembly()?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "FileFlows.exe");

            FileSystemWatcher watcher = new FileSystemWatcher(UpdateDirectory);
            watcher.NotifyFilter =
                             NotifyFilters.Security |
                             NotifyFilters.Attributes |
                             NotifyFilters.CreationTime |
                             NotifyFilters.DirectoryName |
                             NotifyFilters.FileName |
                             NotifyFilters.LastWrite |
                             NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            watcher.Filter = "*.msi";
            watcher.EnableRaisingEvents = true;

            CheckForUpdate();
        }

        protected override void Execute()
        {
            CheckForUpdate();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Logger.Instance.ILog("AutoUpdater: File change detected: " + e.FullPath);
            CheckForUpdate();
        }

        private void CheckForUpdate()
        {
            Logger.Instance.ILog("AutoUpdater: Checking for updates");
            var update = GetUpdate();
            if (string.IsNullOrEmpty(update.Item1))
            {
                Logger.Instance.ILog("AutoUpdater: No updates found");
                return;
            }

            Logger.Instance.ILog("AutoUpdater: Found update: " + update.Item1);
            RunUpdate(update.Item1, update.Item2);
        }

        private (string, string) GetUpdate()
        {
            var rgxVersion = new Regex(@"(?<=(^FileFlows-))([\d]+\.){3}[\d]+(?=(\.msi$))");
            var currentVersion = Version.Parse(Globals.Version);
            foreach (var file in new DirectoryInfo(UpdateDirectory).GetFiles("*.msi"))
            {
                var match = rgxVersion.Match(file.Name);
                if (match.Success == false)
                {
                    Logger.Instance.ILog("AutoUpdater: File does not match version regex: " + file.Name);
                    continue;
                }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                Version version;
                if (Version.TryParse(match.Value, out version) == false)
                {
                    Logger.Instance.ILog("AutoUpdater: Failed to parse version: " + match.Value);
                    continue;
                }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                if (version > currentVersion)
                    return (file.FullName, match.Value);
                else
                {
                    Logger.Instance.ILog($"AutoUpdater: Version '{version} less than current '{currentVersion}'");
                }
            }
            return (string.Empty, string.Empty);
        }

        public void RunUpdate(string msi, string version)
        {
            Logger.Instance.ILog($"AutoUpdater: Running update [{version}: {msi}");
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
            Logger.Instance.ILog("AutoUpdater: Starting bat file update: " + tempFile);
            Process.Start(tempFile);

            Environment.Exit(99);
        }
    }
}
