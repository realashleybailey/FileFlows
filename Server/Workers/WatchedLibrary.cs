using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using System.Text.RegularExpressions;

namespace FileFlows.Server.Workers
{
    public class WatchedLibrary:IDisposable
    {
        private FileSystemWatcher Watcher;
        private bool Changed = true;
        public Library Library { get;private set; } 

        public List<LibraryFile> LibraryFiles { get; private set; }
        public WatchedLibrary(Library library)
        {
            this.Library = library;
            Watcher = new FileSystemWatcher(library.Path);
            Watcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;
            Watcher.IncludeSubdirectories = true;
            Watcher.Changed += Watcher_Changed;
            Watcher.Created += Watcher_Created;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Renamed += Watcher_Renamed;
            Watcher.EnableRaisingEvents = true;
            LibraryFiles = DbHelper.Select<LibraryFile>("lower(Name) like lower(@1)", library.Path + "%").Result.ToList();
        }


        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Logger.Instance?.DLog("File deleted: " + e.FullPath);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Logger.Instance?.DLog("File renamed: " + e.FullPath + " vs " + e.OldFullPath);
            Changed = true;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Logger.Instance?.DLog("File created: " + e.FullPath);
            Changed = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Logger.Instance?.DLog("File changed: " + e.FullPath + " = " + e.ChangeType);
            Changed = true;
        }

        public void Dispose()
        {
            if (Watcher != null)
            {
                Watcher.EnableRaisingEvents = false;
                Watcher.Dispose();
                Watcher = null;
            }
        }

        public bool Scan()
        {
            if (Library.ScanInterval < 10)
                Library.ScanInterval = 60;

            if (Library.Enabled == false)
                return false;
            if (TimeHelper.InSchedule(Library.Schedule) == false)
                return false;

            if (Changed == false && Library.LastScanned > DateTime.UtcNow.AddSeconds(-Library.ScanInterval))
                return false;

            Changed = false;

            if (string.IsNullOrEmpty(Library.Path) || Directory.Exists(Library.Path) == false)
            {
                Logger.Instance?.WLog($"Library '{Library.Name}' path not found: {Library.Path}");
                return false;
            }


            if (Library.Flow == null)
            {
                Logger.Instance?.WLog($"Library '{Library.Name}' flow not found");
                return false;
            }

            var flow = new FlowController().Get(Library.Flow.Uid).Result;
            if (flow == null)
            {
                Logger.Instance?.WLog($"Library '{Library.Name}' flow not found");
                return false;
            }

            int count = LibraryFiles.Count;
            if (Library.Folders)
                ScanForDirs(Library, flow);
            else
                ScanFoFiles(Library, flow);

            new LibraryController().UpdateLastScanned(Library.Uid).Wait();
            return count < LibraryFiles.Count;
        }


        private void ScanForDirs(Library library, Flow flow)
        {
            var dirs = new DirectoryInfo(library.Path).GetDirectories();
            Regex regexFilter = null;
            try
            {
                regexFilter = string.IsNullOrEmpty(library.Filter) ? null : new Regex(library.Filter, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Library '{library.Name}' filter '{library.Filter} is invalid: " + ex.Message);
                return;
            }
            List<string> known = new();

            foreach (var libFile in LibraryFiles)
            {
                if (libFile.IsDirectory == false)
                    continue;
                if (string.IsNullOrEmpty(libFile.Name) == false && known.Contains(libFile.Name.ToLower()) == false)
                    known.Add(libFile.Name.ToLower());
            }
            var tasks = new List<Task<LibraryFile>>();
            foreach (var dir in dirs)
            {
                if (regexFilter != null && regexFilter.IsMatch(dir.FullName) == false)
                    continue;

                if (known.Contains(dir.FullName.ToLower()))
                    continue; // already known

                tasks.Add(GetLibraryFile(library, flow, dir));
            }
            Task.WaitAll(tasks.ToArray());

            new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();
        }
        private void ScanFoFiles(Library library, Flow flow)
        {
            var files = GetFiles(new DirectoryInfo(library.Path));
            Regex? regexFilter = null;
            try
            {
                regexFilter = string.IsNullOrEmpty(library.Filter) ? null : new Regex(library.Filter, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Library '{library.Name}' filter '{library.Filter} is invalid: " + ex.Message);
                return;
            }
            List<string> known = new();

            foreach (var libFile in LibraryFiles)
            {
                if (libFile.IsDirectory == true)
                    continue;
                if (string.IsNullOrEmpty(libFile.Name) == false && known.Contains(libFile.Name.ToLower()) == false)
                    known.Add(libFile.Name.ToLower());
                // need to also exclude the final output of a library file
                if (string.IsNullOrEmpty(libFile.OutputPath) == false && known.Contains(libFile.OutputPath.ToLower()) == false)
                    known.Add(libFile.OutputPath.ToLower());
            }
            var tasks = new List<Task<LibraryFile>>();
            foreach (var file in files)
            {
                if (regexFilter != null && regexFilter.IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
                    continue;

                if (known.Contains(file.FullName.ToLower()))
                    continue; // already known

                tasks.Add(GetLibraryFile(library, flow, file));
            }
            Task.WaitAll(tasks.ToArray());

            new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();
        }


        private async Task<LibraryFile> GetLibraryFile(Library library, Flow flow, FileSystemInfo info)
        {
            if (info is FileInfo fileInfo)
            {
                if (await CanAccess(fileInfo, library.FileSizeDetectionInterval) == false)
                    return null; // this can happen if the file is currently being written to, next scan will retest it
            }

            var lf = new LibraryFile
            {
                Name = info.FullName,
                RelativePath = info.FullName.Substring(library.Path.Length + 1),
                Status = FileStatus.Unprocessed,
                IsDirectory = info is DirectoryInfo,
                Library = new ObjectReference
                {
                    Name = library.Name,
                    Uid = library.Uid,
                    Type = library.GetType()?.FullName ?? string.Empty
                },
                Flow = new ObjectReference
                {
                    Name = flow.Name,
                    Uid = flow.Uid,
                    Type = flow.GetType()?.FullName ?? string.Empty
                },
                Order = -1
            };

            LibraryFiles.Add(lf);
            return lf;
        }


        private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
        {
            try
            {
                if (file.LastAccessTimeUtc < DateTime.UtcNow.AddSeconds(-10))
                {
                    // check if the file size changes
                    long fs = file.Length;
                    if (fileSizeDetectionInterval > 0)
                        await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

                    if (fs != file.Length)
                    {
                        Logger.Instance.ILog("File size has changed, skipping for now: " + file.FullName);
                        return false; // file size has changed, could still be being written too
                    }
                }
                using (var fs = File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite))
                {
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public List<FileInfo> GetFiles(DirectoryInfo dir)
        {
            var files = new List<FileInfo>();
            try
            {
                foreach (var subdir in dir.GetDirectories())
                {
                    files.AddRange(GetFiles(subdir));
                }
                files.AddRange(dir.GetFiles());
            }
            catch (Exception) { }
            return files;
        }
    }
}
