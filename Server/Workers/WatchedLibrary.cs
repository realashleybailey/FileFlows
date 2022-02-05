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
        public Library Library { get;private set; } 

        public List<LibraryFile> LibraryFiles { get; private set; }

        private Regex? Filter;

        private bool ScanComplete = false;
        private bool UseScanner = false;

        public WatchedLibrary(Library library)
        {
            this.Library = library;
            this.UseScanner = library.Scan;
            if(string.IsNullOrEmpty(library.Filter) == false)
            {
                try
                {
                    Filter = new Regex(library.Filter, RegexOptions.IgnoreCase);
                }
                catch (Exception) { }
            }
            RefreshLibraryFiles();
            if(UseScanner == false)
                SetupWatcher();
        }

        private void RefreshLibraryFiles()
        {
            if(string.IsNullOrEmpty(this.Library?.Path) == false)
                LibraryFiles = DbHelper.Select<LibraryFile>("lower(Name) like lower(@1)", this.Library.Path + "%").Result.ToList();
        }
        public void Dispose()
        {
            DisposeWatcher();
        }

        void SetupWatcher()
        {
            DisposeWatcher();

            Watcher = new FileSystemWatcher(Library.Path);
            Watcher.NotifyFilter =
                             //NotifyFilters.Attributes |
                             NotifyFilters.CreationTime |
                             NotifyFilters.DirectoryName |
                             NotifyFilters.FileName |
                             // NotifyFilters.LastAccess |
                             NotifyFilters.LastWrite |
                             //| NotifyFilters.Security
                             NotifyFilters.Size;
            Watcher.IncludeSubdirectories = true;
            Watcher.Changed += Watcher_Changed;
            Watcher.Created += Watcher_Changed;
            //Watcher.Deleted += Watcher_Changed;
            Watcher.Renamed += Watcher_Changed;
            Watcher.EnableRaisingEvents = true;

        }

        void DisposeWatcher()
        {
            if (Watcher != null)
            {
                Watcher.Changed -= Watcher_Changed;
                Watcher.Created -= Watcher_Changed;
                Watcher.Renamed -= Watcher_Changed;
                Watcher.EnableRaisingEvents = false;
                Watcher.Dispose();
                Watcher = null;
            }
        }

        private bool IsMatch(string input)
        {
            if (Filter == null)
                return true;
            return Filter.IsMatch(input);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Library.Folders == false && Directory.Exists(e.FullPath))
                {
                    foreach (var file in Directory.GetFiles(e.FullPath, "*.*", SearchOption.AllDirectories))
                    {
                        FileChangeEvent(file);
                    }
                }
                else
                {
                    var file = new FileInfo(e.FullPath);
                    if (file.Exists == false)
                        return;
                                        
                    long size = file.Length;
                    Thread.Sleep(20_000);
                    if (size < file.Length)
                        return; // if the file is being copied, we need to wait for that to finish, which will fire a new event

                    FileChangeEvent(e.FullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("WatchedLibrary.Watched Exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void FileChangeEvent(string fullPath)
        { 
            if (IsMatch(fullPath) == false)
            {
                if (fullPath.Contains("_UNPACK_"))
                    return; // dont log this, too many
                return;
            }

            // new file we can process
            var known = KnownFile(fullPath);
            if (known.libFile != null)
            {
                if(known.libFile.Name == fullPath)
                    return; // complete duplciate
                // duplicate file or file was moved, we treat this as a new entry to avoid scanning it again and again
            }

            Logger.Instance.ILog("WatchedLibrary: Changed event detected on file: " + fullPath);

            if (IsFileLocked(fullPath) == false)
            {
                Logger.Instance.ILog("WatchedLibrary: Detected new file: " + fullPath);
                _ = AddLibraryFile(fullPath, known.fingerprint, known.libFile);
            }
            else
            {
                Logger.Instance.ILog("WatchedLibrary: New file detected, but currently locked: " + fullPath);
            }
        }

        internal void UpdateLibrary(Library library)
        {
            this.Library = library;

            if (UseScanner && library.Scan == false)
            {
                Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to watched mode, starting watcher");
                UseScanner = false;
                SetupWatcher();
            }
            else if(UseScanner == false && library.Scan == true)
            {
                Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to scan mode, disposing watcher");
                UseScanner = true;
                DisposeWatcher();
            }
            else if(UseScanner == false && Watcher != null && Watcher.Path != library.Path)
            {
                // library path changed, need to change watcher
                Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' path changed, updating watched path");
                SetupWatcher(); 
            }

            if (library.Enabled && library.LastScanned < new DateTime(2020, 1, 1))
            {
                ScanComplete = false; // this could happen if they click "Rescan" on the library page, this will force a full new scan
                Logger.Instance?.ILog($"WatchedLibrary: Library '{library.Name}' marked for full scan");
            }
        }


        private (LibraryFile? libFile, string fingerprint) KnownFile(string file)
        {
            file = file.ToLower();
            var libFile = LibraryFiles.Where(x => x.Name.ToLower() == file).FirstOrDefault();
            if (libFile != null)
                return (libFile, libFile.Fingerprint ?? string.Empty);

            if (Library.UseFingerprinting)
            {
                string fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(file);
                if(string.IsNullOrEmpty(fingerprint) == false)
                {
                    var existing = LibraryFiles.Where(x => x.Fingerprint == fingerprint).OrderBy(x => x.Status).FirstOrDefault();
                    if (existing != null)
                        return (existing, fingerprint);
                }
                return (null, fingerprint);
            }

            return (null, string.Empty);
        }

        private bool IsFileLocked(string file)
        {
            const int ERROR_SHARING_VIOLATION = 32;
            const int ERROR_LOCK_VIOLATION = 33;

            if (File.Exists(file))
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception ex2)
                {
                    //_log.WriteLog(ex2, "Error in checking whether file is locked " + file);
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex2) & ((1 << 16) - 1);
                    if ((ex2 is IOException) && (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION))
                    {
                        return true;
                    }
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

        public bool Scan(bool fullScan = false)
        {
            if (Library.ScanInterval < 10)
                Library.ScanInterval = 60;

            if (Library.Enabled == false)
                return false;
            if (TimeHelper.InSchedule(Library.Schedule) == false)
                return false;

            if (fullScan == false)
                fullScan = Library.LastScanned < DateTime.Now.AddHours(-1); // do a full scan every hour just incase we missed something

            if (fullScan == false && Library.LastScanned > DateTime.Now.AddSeconds(-Library.ScanInterval))
                return false;

            if (UseScanner == false && ScanComplete && fullScan == false)
            {
                //Logger.Instance?.ILog($"Library '{Library.Name}' has full scan, using FileWatcherEvents now to watch for new files");
                return false; // we can use the filesystem watchers for any more files
            }

            if (string.IsNullOrEmpty(Library.Path) || Directory.Exists(Library.Path) == false)
            {
                Logger.Instance?.WLog($"WatchedLibrary: Library '{Library.Name}' path not found: {Library.Path}");
                return false;
            }

            // refresh list of library files
            RefreshLibraryFiles();

            bool complete = true;
            int count = LibraryFiles.Count;
            if (Library.Folders)
                ScanForDirs(Library);
            else
                complete = ScanForFiles(Library);

            if(complete)
                ScanComplete = Library.Folders == false; // only count a full scan against files

            if (ScanComplete)
                new LibraryController().UpdateLastScanned(Library.Uid).Wait();

            return count < LibraryFiles.Count;
        }


        private void ScanForDirs(Library library)
        {
            var dirs = new DirectoryInfo(library.Path).GetDirectories();
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
                if (IsMatch(dir.FullName) == false)
                    continue;

                if (known.Contains(dir.FullName.ToLower()))
                    continue; // already known

                tasks.Add(GetLibraryFile(dir, string.Empty));
            }
            Task.WaitAll(tasks.ToArray());

            new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();
        }
        private bool ScanForFiles(Library library)
        {
            Logger.Instance.DLog("Started searching directory for files: " + library.Path);
            var files = GetFiles(new DirectoryInfo(library.Path));
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
            bool incomplete = false;
            var tasks = new List<Task<LibraryFile>>();
            foreach (var file in files)
            {
                if (IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
                    continue;

                if (known.Contains(file.FullName.ToLower()))
                    continue; // already known

                if (tasks.Count() > 250)
                {
                    incomplete = true;
                    break; // bucket how many are scanned at once
                }

                Logger.Instance.DLog("New unknown file: " + file.FullName);
                var task = Task.Run(async () => await GetLibraryFile(file, null));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            new LibraryFileController().AddMany(tasks.Where(x => x.Result != null).Select(x => x.Result).ToArray()).Wait();

            return incomplete == false;
        }

        private async Task AddLibraryFile(string filename, string fingerprint, LibraryFile duplicate)
        {
            var result = await GetLibraryFile(new FileInfo(filename), fingerprint);
            if (duplicate != null)
            {
                result.Status  = FileStatus.Duplicate;
                result.Duplicate = new ObjectReference
                {
                    Name = duplicate.Name,
                    Uid = duplicate.Uid,
                    Type = duplicate.GetType().FullName
                };
            }
            if(result != null)
                await new LibraryFileController().AddMany(new[] { result });
        }

        private Flow GetFlow()
        {
            var flow = new FlowController().Get(Library.Flow.Uid).Result;
            if (flow == null)
            {
                Logger.Instance?.WLog($"WatchedLibrary: Library '{Library.Name}' flow not found");
                return null;
            }
            return flow;
        }

        private async Task<LibraryFile> GetLibraryFile(FileSystemInfo info, string fingerprint)
        {
            long size = 0;
            if (info is FileInfo fileInfo)
            {
                if (await CanAccess(fileInfo, Library.FileSizeDetectionInterval) == false)
                {
                    Logger.Instance.DLog("Cannot access file: " + info.FullName);
                    return null; // this can happen if the file is currently being written to, next scan will retest it
                }
                size = fileInfo.Length;
            }

            int skip = Library.Path.Length;
            // check if the length is != 3 incase its jusst a directory, eg "Z:\"
            // else if the path is windows and just "Z:" we will include the "\" to skip by increasing the skip count
            // else its in a folder and we have to increase the skip by 1 to add the directory separator
            if (Globals.IsWindows == false || Library.Path.Length != 3)
                ++skip;

            var status = FileStatus.Unprocessed;
            ObjectReference duplicate = new ObjectReference();
            if (fingerprint == null && Library.UseFingerprinting)
            {
                fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(info.FullName);
                if (string.IsNullOrWhiteSpace(fingerprint) == false)
                {
                    // check if the fingerprint already exists
                    var existing = (await new LibraryFileController().GetDataList()).Where(x => x.Fingerprint == fingerprint).FirstOrDefault();
                    if(existing != null)
                    {
                        Logger.Instance.ILog("Duplicate library file found: " + existing.Name);
                        status = FileStatus.Duplicate;
                        duplicate = new ObjectReference
                        {
                            Name = existing.Name,
                            Uid = existing.Uid,
                            Type = existing.GetType().FullName
                        };
                    }
                }
                
            }

            string relative = info.FullName.Substring(skip);
            var lf = new LibraryFile
            {
                Name = info.FullName,
                RelativePath = relative,
                Status = status,
                IsDirectory = info is DirectoryInfo,
                Fingerprint = fingerprint ?? string.Empty,
                Duplicate = duplicate,
                OriginalSize = size,
                Library = new ObjectReference
                {
                    Name = Library.Name,
                    Uid = Library.Uid,
                    Type = Library.GetType()?.FullName ?? string.Empty
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
                if (file.LastAccessTime < DateTime.Now.AddSeconds(-10))
                {
                    // check if the file size changes
                    long fs = file.Length;
                    if (fileSizeDetectionInterval > 0)
                        await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

                    if (fs != file.Length)
                    {
                        Logger.Instance.ILog("WatchedLibrary: File size has changed, skipping for now: " + file.FullName);
                        return false; // file size has changed, could still be being written too
                    }
                }

                using (var fs = File.Open(file.FullName, FileMode.Open))
                {
                    if(fs.CanRead == false)
                    {
                        Logger.Instance.ILog("Cannot read file: " + file.FullName);
                        return false;
                    }
                    if (fs.CanWrite == false)
                    {
                        Logger.Instance.ILog("Cannot write file: " + file.FullName);
                        return false;
                    }
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
