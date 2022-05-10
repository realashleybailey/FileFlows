// disabling for now as switched to a nsis/exe installer

// namespace FileFlows.Server.Workers
// {
//     using FileFlows.Server.Controllers;
//     using FileFlows.ServerShared.Workers;
//     using FileFlows.Shared;
//     using FileFlows.Shared.Helpers;
//     using System.Diagnostics;
//     using System.Reflection;
//     using System.Text.RegularExpressions;
//
//     /// <summary>
//     /// A worker that automatically updates FileFlows
//     /// </summary>
//     public class AutoUpdater : Worker
//     {
//         private static string _UpdateDirectory;
//         private static string UpdateDirectory
//         {
//             get
//             {
//                 if (string.IsNullOrEmpty(_UpdateDirectory))
//                 {
//                     _UpdateDirectory = Path.Combine(DirectoryHelper.BaseDirectory, "Updates");
//                 }
//                 return _UpdateDirectory;
//             }
//         }
//
//         private DateTime LastCheckedOnline = DateTime.MinValue;
//         private int LastCheckedOnlineIntervalMinutes = 60; // 60 minutes
//
//         private static bool DevTest = false;
//
//         /// <summary>
//         /// Gets or sets if a updating is pending installation
//         /// </summary>
//         public static bool UpdatePending { get; private set; }
//
//         public AutoUpdater() : base(ScheduleType.Minute, 1)
//         {
//             Logger.Instance.ILog("AutoUpdater: Starting AutoUpdater");
//
//             DevTest = File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "devtest"));
//             if (DevTest)
//             {
//                 LastCheckedOnlineIntervalMinutes = 2;
//             }
//
//             if (Directory.Exists(UpdateDirectory) == false)
//             {
//                 Logger.Instance.ILog("AutoUpdater: Creating updates directory: " + UpdateDirectory);
//                 Directory.CreateDirectory(UpdateDirectory);
//             }
//             else
//             {
//                 CleanUpOldFiles();
//                 Logger.Instance.ILog("AutoUpdater: Watch Directory: " + UpdateDirectory);
//             }
//
//             FileSystemWatcher watcher = new FileSystemWatcher(UpdateDirectory);
//             watcher.NotifyFilter =
//                              NotifyFilters.FileName |
//                              NotifyFilters.DirectoryName |
//                              NotifyFilters.Attributes |
//                              NotifyFilters.Size |
//                              NotifyFilters.LastWrite |
//                              NotifyFilters.LastAccess |
//                              NotifyFilters.CreationTime |
//                              NotifyFilters.Security;
//
//             watcher.Changed += Watcher_Changed;
//             watcher.Created += Watcher_Changed;
//             watcher.Renamed += Watcher_Changed;
//             
//             watcher.EnableRaisingEvents = true;
//
//             Execute();
//         }
//
//         protected override void Execute()
//         {
//             var settings = new SettingsController().Get().Result;
//             if (settings.AutoUpdate == false)
//                 return;
//
//             if (LastCheckedOnline < DateTime.Now.AddMinutes(-LastCheckedOnlineIntervalMinutes).AddSeconds(5))
//             {
//                 CheckForUpdateOnline();
//                 LastCheckedOnline = DateTime.Now;
//             }
//
//             CheckForUpdate();
//         }
//
//         public static (bool updateAvailable, Version onlineVersion) GetLatestOnlineVersion()
//         {
//             try
//             {
//                 string url = "https://fileflows.com/api/telemetry/latest-version";
//                 if (DevTest)
//                     url += "?devtest=true";
//                 var result = HttpHelper.Get<string>(url, noLog: true).Result;
//                 if (result.Success == false)
//                 {
//                     Logger.Instance.ILog("AutoUpdater: Failed to retrieve online version");
//                     return (false, new Version(0,0,0,0));
//                 }
//
//                 Version current = Version.Parse(Globals.Version);
//                 Version? onlineVersion;
//                 if (Version.TryParse(result.Data, out onlineVersion) == false)
//                 {
//                     Logger.Instance.ILog("AutoUpdater: Failed to parse online version: " + result.Data);
//                     return (false, new Version(0, 0, 0, 0));
//                 }
//                 if (current >= onlineVersion)
//                 {
//                     Logger.Instance.ILog($"AutoUpdater: Current version '{current}' newer or same as online version '{onlineVersion}'");
//                     return (false, onlineVersion);
//                 }
//                 return (true, onlineVersion);
//             }
//             catch (Exception ex)
//             {
//                 Logger.Instance.ELog("AutoUpdater: Failed checking online version: " + ex.Message);
//                 return (false, new Version(0, 0, 0, 0));
//             }
//         }
//
//         private void CheckForUpdateOnline()
//         {
//             try
//             {
//                 var result = GetLatestOnlineVersion();
//                 if (result.updateAvailable == false)
//                     return;
//
//                 Version onlineVersion = result.onlineVersion;
//
//                 string file = Path.Combine(UpdateDirectory, $"FileFlows-{onlineVersion}.msi");
//                 if (File.Exists(file))
//                 {
//                     Logger.Instance.ILog("AutoUpdater: Update already downloaded: " + file);
//                     return;
//                 }
//
//                 Logger.Instance.ILog("AutoUpdater: Downloading update: " + onlineVersion);
//                 DownloadFile(file);
//             }
//             catch (Exception ex)
//             {
//                 Logger.Instance.ELog("AutoUpdater: Failed checking online version: " + ex.Message);
//             }
//         }
//
//         private void DownloadFile(string file)
//         {
//             string downloadUrl = "https://fileflows.com/downloads/server-msi?ts=" + DateTime.Now.Ticks;
//             if (DevTest)
//                 downloadUrl += "&devtest=true";
//
// #pragma warning disable SYSLIB0014 // Type or member is obsolete
//             using (var client = new System.Net.WebClient())
//             {
//                 client.DownloadFile(downloadUrl, file);
//             }
// #pragma warning restore SYSLIB0014 // Type or member is obsolete
//         }
//
//         private void Watcher_Changed(object sender, FileSystemEventArgs e)
//         {
//             Logger.Instance.ILog("AutoUpdater: File change detected: " + e.FullPath);
//             if (e.FullPath.EndsWith(".msi") == false)
//                 return;
//             CheckForUpdate();
//         }
//
//         private void CheckForUpdate()
//         {
//             Logger.Instance.ILog("AutoUpdater: Checking for updates");
//             var update = GetUpdate();
//             if (string.IsNullOrEmpty(update.Item1))
//             {
//                 Logger.Instance.ILog("AutoUpdater: No updates found");
//                 return;
//             }
//
//             Logger.Instance.ILog("AutoUpdater: Found update: " + update.Item1);
//
//             UpdatePending = true;
//
//             var workers = new WorkerController(null).GetAll();
//             bool canUpdate = workers?.Any() != true;
//             if(canUpdate == false)
//             {
//                 Logger.Instance.ILog("AutoUpdater: Currently processing files, cannot update");
//                 return;
//             }
//
//             Logger.Instance.ILog("AutoUpdater: Currently not processing files, can update");
//             RunUpdate(update.Item1, update.Item2);
//         }
//
//         private (string, string) GetUpdate()
//         {
//             foreach (var file in new DirectoryInfo(UpdateDirectory).GetFiles("*.msi"))
//             {
//                 var isGreater = IsGreaterThanCurrent(file.FullName);
//                 if (isGreater.greater == true)
//                     return (file.FullName, isGreater.version);
//             }
//             return (string.Empty, string.Empty);
//         }
//
//         private static (bool greater, string version) IsGreaterThanCurrent(string filename)
//         {
//             string shortName = new FileInfo(filename).Name;
//             var rgxVersion = new Regex(@"(?<=(^FileFlows-))([\d]+\.){3}[\d]+(?=(\.msi$))");
//             var currentVersion = Version.Parse(Globals.Version);
//             var match = rgxVersion.Match(shortName);
//             if (match.Success == false)
//             {
//                 Logger.Instance.ILog("AutoUpdater: File does not match version regex: " + filename);
//                 return (false, string.Empty);
//             }
//
// #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
//             Version version;
//             if (Version.TryParse(match.Value, out version) == false)
//             {
//                 Logger.Instance.ILog("AutoUpdater: Failed to parse version: " + match.Value);
//                 return (false, string.Empty); ;
//             }
// #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
//
//             if (version > currentVersion)
//                 return (true, match.Value);
//             return (false, match.Value);
//         }
//
//         public void RunUpdate(string msi, string version)
//         {
//             Logger.Instance.ILog($"AutoUpdater: Running update [{version}]: {msi}");
//             Process.Start("msiexec.exe", $"/i \"{msi}\" /quiet /qn");
//
//             WorkerManager.StopWorkers();
//             Environment.Exit(99);
//         }
//
//         internal static void CleanUpOldFiles(int delayMilliseconds = 30_000)
//         {
//             try
//             {
//                 string dir = UpdateDirectory;
//                 if (Directory.Exists(dir) == false)
//                     return;
//
//                 _ = Task.Run(async () =>
//                 {
//                     try
//                     {
//                         await Task.Delay(delayMilliseconds);
//
//                         foreach (var file in Directory.GetFiles(dir, "FileFlows-*.msi"))
//                         {
//                         // check if version greater than this
//                         var isGreater = IsGreaterThanCurrent(file);
//                             if (isGreater.greater)
//                                 continue; // dont delete
//                         try
//                             {
//                             // maybe locked
//                             File.Delete(file);
//                                 Logger.Instance.ILog("AutoUpdater: Deleting old update file");
//                             }
//                             catch (Exception) { }
//                         }
//                     }
//                     catch (Exception) { }
//                 });
//             }catch (Exception) { }   
//         }
//     }
// }
