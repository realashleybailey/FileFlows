using System.Text.RegularExpressions;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers
{
    public class LibraryWorker : Worker
    {
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

        protected override void Execute()
        {
            var settings = DbHelper.Single<Settings>();
            if (settings?.WorkerScanner != true)
            {
                Logger.Instance.DLog("Scanner worker not enabled");
                return;
            }
            Logger.Instance.DLog("################### Library worker triggered");
            var libController = new Controllers.LibraryController();
            var libaries = libController.GetAll();
            foreach (var library in libaries)
            {
                if (library.Enabled == false)
                    continue;
                if (string.IsNullOrEmpty(library.Path) || Directory.Exists(library.Path) == false)
                {
                    Logger.Instance.WLog($"Library '{library.Name}' path not found: {library.Path}");
                    continue;
                }

                var flow = library.Flow == null ? null : DbHelper.Single<Flow>(library.Flow.Uid);
                if (flow == null || flow.Uid == Guid.Empty)
                {
                    Logger.Instance.WLog($"Library '{library.Name}' flow not found");
                    continue;
                }

                var files = GetFiles(new DirectoryInfo(library.Path));
                Regex regexFilter = null;
                try
                {
                    regexFilter = string.IsNullOrEmpty(library.Filter) ? null : new Regex(library.Filter, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    Logger.Instance.WLog($"Library '{library.Name}' filter '{library.Filter} is invalid: " + ex.Message);
                    continue;
                }
                string[] known = DbHelper.GetNames<LibraryFile>().ToArray();
                foreach (var file in files)
                {
                    if (regexFilter != null && regexFilter.IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
                        continue;

                    if (known.Contains(file.FullName))
                        continue; // already known

                    if (CanAccess(file) == false)
                        continue; // this can happen if the file is currently being written to, next scan will retest it

                    var libraryFile = new LibraryFile
                    {
                        Name = file.FullName,
                        RelativePath = file.FullName.Substring(library.Path.Length + 1),
                        Status = FileStatus.Unprocessed,
                        Library = new ObjectReference
                        {
                            Name = library.Name,
                            Uid = library.Uid,
                            Type = library.GetType().FullName
                        },
                        Flow = new ObjectReference
                        {
                            Name = flow.Name,
                            Uid = flow.Uid,
                            Type = flow.GetType().FullName
                        },
                        Order = -1
                    };
                    DbHelper.Update(libraryFile);
                    Logger.Instance.DLog("Found file to process: " + file);
                }
            }
            Logger.Instance.DLog("Finished scanning libraries");
        }

        internal static void ResetProcessing()
        {
            var processing = DbHelper.Select<LibraryFile>()
                                     .Where(x => x.Status == FileStatus.Processing);
            foreach (var p in processing)
            {
                p.Status = FileStatus.Unprocessed;
                DbHelper.Update(p);
            }
        }

        private bool CanAccess(FileInfo file)
        {
            try
            {
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