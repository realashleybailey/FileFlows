namespace FileFlows.Server.Workers
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Workers;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    public class AutoUpdater : Worker
    {
        private static string UpdateDirectory;
        private static string WindowsServerExe;

        public AutoUpdater() : base(ScheduleType.Minute, 1)
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
                Logger.Instance.ILog("AutoUpdater: Watch Directory: " + UpdateDirectory);
            }

            WindowsServerExe = Assembly.GetExecutingAssembly()?.Location ?? Path.Combine(Directory.GetCurrentDirectory(), "FileFlows.exe");

            FileSystemWatcher watcher = new FileSystemWatcher(UpdateDirectory);
            watcher.NotifyFilter =
                             NotifyFilters.FileName |
                             NotifyFilters.DirectoryName |
                             NotifyFilters.Attributes |
                             NotifyFilters.Size |
                             NotifyFilters.LastWrite |
                             NotifyFilters.LastAccess |
                             NotifyFilters.CreationTime |
                             NotifyFilters.Security;

            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            
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
            if (e.FullPath.EndsWith(".msi") == false)
                return;
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

            var workers = new WorkerController(null).GetAll();
            bool canUpdate = workers?.Any() != true;
            if(canUpdate == false)
            {
                Logger.Instance.ILog("AutoUpdater: Currently processing files, cannot update");
                return;
            }

            Logger.Instance.ILog("AutoUpdater: Currently not processing files, can update");
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
            Process.Start(tempFile, $"> \"{tempFile}.log\"");
            Environment.Exit(99);
        }
    }
}
