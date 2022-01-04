using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers
{
    public class LibraryWorker : Worker
    {
        private Dictionary<string, WatchedLibrary> WatchedLibraries = new ();

        public LibraryWorker() : base(ScheduleType.Second, 10)
        {
            Trigger();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        private void Watch(params Library[] libraries)
        {
            foreach(var lib in libraries)
            {
                WatchedLibraries.Add(lib.Uid + ":" + lib.Path, new WatchedLibrary(lib));
            }
        }

        private void Unwatch(params string[] keys)
        {
            foreach(string key in keys)
            {
                if (WatchedLibraries.ContainsKey(key) == false)
                    continue;
                var watcher = WatchedLibraries[key];
                watcher.Dispose();
                WatchedLibraries.Remove(key);
                watcher = null;
            }
        }

        protected override void Execute()
        {
            var libController = new LibraryController();
            var libraries = libController.GetAll().Result;
            var libraryUids = libraries.Select(x => x.Uid + ":" + x.Path).ToList();


            Watch(libraries.Where(x => WatchedLibraries.ContainsKey(x.Uid + ":" + x.Path) == false).ToArray());
            Unwatch(WatchedLibraries.Keys.Where(x => libraryUids.Contains(x) == false).ToArray());

            //var libFileController = new LibraryFileController();
            //List<LibraryFile> libraryFiles = libFileController.GetAll(null).Result?.ToList() ?? new ();
            //Dictionary<Guid, Flow> flows = new FlowController().GetData().Result;

            bool scannedAny = false;
            foreach(var libwatcher in WatchedLibraries.Values)
            {
                scannedAny |= libwatcher.Scan();
            }
            //foreach (var library in libraries)
            //{
            //    if (library.ScanInterval < 10)
            //        library.ScanInterval = 60;

            //    if (library.Enabled == false)
            //        continue;
            //    if(library.LastScanned > DateTime.UtcNow.AddSeconds(-library.ScanInterval))
            //        continue;

            //    if (TimeHelper.InSchedule(library.Schedule) == false)
            //        continue;

            //    if (string.IsNullOrEmpty(library.Path) || Directory.Exists(library.Path) == false)
            //    {
            //        Logger.Instance.WLog($"Library '{library.Name}' path not found: {library.Path}");
            //        continue;
            //    }

            //    if (library.Flow == null || flows.ContainsKey(library.Flow.Uid) == false)
            //    {
            //        Logger.Instance.WLog($"Library '{library.Name}' flow not found");
            //        continue;
            //    }
            //    scannedAny = true;
            //    var flow = flows[library.Flow.Uid];

            //    if (library.Folders)
            //        ScanForDirs(library, libraryFiles, flow);
            //    else
            //        ScanFoFiles(library, libraryFiles, flow);

            //    libController.UpdateLastScanned(library.Uid).Wait();
            //}
            if(scannedAny)
                Logger.Instance.DLog("Finished scanning libraries");
        }

        internal static void ResetProcessing()
        {
            // special case can use dbhelper directly
            // this is called at the start up of FileFlows
            var processing = DbHelper.Select<LibraryFile>().Result.Where(x => x.Status == FileStatus.Processing);
            foreach (var p in processing)
            {
                p.Status = FileStatus.Unprocessed;
                DbHelper.Update(p).Wait();
            }
        }

        //private void ScanForDirs(Library library, List<LibraryFile> libraryFiles, Flow flow)
        //{
        //    var dirs = new DirectoryInfo(library.Path).GetDirectories();
        //    Regex regexFilter = null;
        //    try
        //    {
        //        regexFilter = string.IsNullOrEmpty(library.Filter) ? null : new Regex(library.Filter, RegexOptions.IgnoreCase);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Instance.WLog($"Library '{library.Name}' filter '{library.Filter} is invalid: " + ex.Message);
        //        return;
        //    }
        //    List<string> known = new();

        //    foreach (var libFile in libraryFiles)
        //    {
        //        if (libFile.IsDirectory == false)
        //            continue;
        //        if (string.IsNullOrEmpty(libFile.Name) == false && known.Contains(libFile.Name.ToLower()) == false)
        //            known.Add(libFile.Name.ToLower());
        //    }
        //    var tasks = new List<Task<LibraryFile>>();
        //    foreach (var dir in dirs)
        //    {
        //        if (regexFilter != null && regexFilter.IsMatch(dir.FullName) == false)
        //            continue;

        //        if (known.Contains(dir.FullName.ToLower()))
        //            continue; // already known

        //        tasks.Add(GetLibraryFile(library, flow, dir));
        //    }
        //    Task.WaitAll(tasks.ToArray());

        //    new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();
        //}
        //private void ScanFoFiles(Library library, List<LibraryFile> libraryFiles, Flow flow)
        //{
        //    var files = GetFiles(new DirectoryInfo(library.Path));
        //    Regex regexFilter = null;
        //    try
        //    {
        //        regexFilter = string.IsNullOrEmpty(library.Filter) ? null : new Regex(library.Filter, RegexOptions.IgnoreCase);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Instance.WLog($"Library '{library.Name}' filter '{library.Filter} is invalid: " + ex.Message);
        //        return;
        //    }
        //    List<string> known = new();

        //    foreach (var libFile in libraryFiles)
        //    {
        //        if (libFile.IsDirectory == true)
        //            continue;
        //        if (string.IsNullOrEmpty(libFile.Name) == false && known.Contains(libFile.Name.ToLower()) == false)
        //            known.Add(libFile.Name.ToLower());
        //        // need to also exclude the final output of a library file
        //        if (string.IsNullOrEmpty(libFile.OutputPath) == false && known.Contains(libFile.OutputPath.ToLower()) == false)
        //            known.Add(libFile.OutputPath.ToLower());
        //    }
        //    var tasks = new List<Task<LibraryFile>>();
        //    foreach (var file in files)
        //    {
        //        if (regexFilter != null && regexFilter.IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
        //            continue;

        //        if (known.Contains(file.FullName.ToLower()))
        //            continue; // already known

        //        tasks.Add(GetLibraryFile(library, flow, file));
        //    }
        //    Task.WaitAll(tasks.ToArray());

        //    new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();
        //}

        //private async Task<LibraryFile?> GetLibraryFile(Library library, Flow flow, FileSystemInfo info)
        //{
        //    if (info is FileInfo fileInfo)
        //    {
        //        if (await CanAccess(fileInfo, library.FileSizeDetectionInterval) == false)
        //            return null; // this can happen if the file is currently being written to, next scan will retest it
        //    }

        //    return new LibraryFile
        //    {
        //        Name = info.FullName,
        //        RelativePath = info.FullName.Substring(library.Path.Length + 1),
        //        Status = FileStatus.Unprocessed,
        //        IsDirectory = info is DirectoryInfo,
        //        Library = new ObjectReference
        //        {
        //            Name = library.Name,
        //            Uid = library.Uid,
        //            Type = library.GetType()?.FullName ?? string.Empty
        //        },
        //        Flow = new ObjectReference
        //        {
        //            Name = flow.Name,
        //            Uid = flow.Uid,
        //            Type = flow.GetType()?.FullName ?? string.Empty
        //        },
        //        Order = -1
        //    };
        //}

        //private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
        //{
        //    try
        //    {
        //        if (file.LastAccessTimeUtc < DateTime.UtcNow.AddSeconds(-10))
        //        {
        //            // check if the file size changes
        //            long fs = file.Length;
        //            if(fileSizeDetectionInterval > 0)
        //                await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

        //            if (fs != file.Length)
        //            {
        //                Logger.Instance.ILog("File size has changed, skipping for now: " + file.FullName);
        //                return false; // file size has changed, could still be being written too
        //            }
        //        }
        //        using (var fs = File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite))
        //        {
        //        }

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public List<FileInfo> GetFiles(DirectoryInfo dir)
        //{
        //    var files = new List<FileInfo>();
        //    try
        //    {
        //        foreach (var subdir in dir.GetDirectories())
        //        {
        //            files.AddRange(GetFiles(subdir));
        //        }
        //        files.AddRange(dir.GetFiles());
        //    }
        //    catch (Exception) { }
        //    return files;
        //}
    }
}