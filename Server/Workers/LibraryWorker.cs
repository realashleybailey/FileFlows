using System.Text.RegularExpressions;
using FileFlow.Shared.Models;

namespace FileFlow.Server.Workers
{
    public class LibraryWorker : Worker
    {
        public readonly Dictionary<Guid, List<string>> Queue = new();
        public readonly List<string> Processed = new List<string>();

        public FlowWorker FlowWorker = new FlowWorker();

        public LibraryWorker() : base(ScheduleType.Minute, 1)
        {
            Trigger();
        }

        public override void Start()
        {
            base.Start();
            FlowWorker.Start();
        }

        public override void Stop()
        {
            base.Stop();
            FlowWorker.Stop();
        }

        protected override void Execute()
        {
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
                foreach (var file in files)
                {
                    if (regexFilter != null && regexFilter.IsMatch(file.FullName) == false)
                        continue;
                    if (Processed.Contains(file.FullName))
                        continue;
                    List<string> list;
                    lock (Queue)
                    {
                        if (Queue.ContainsKey(library.Uid) == false)
                            Queue.Add(library.Uid, new List<string>());
                        list = Queue[library.Uid];
                    }
                    if (list.Contains(file.FullName))
                        continue;
                    list.Add(file.FullName);
                    Logger.Instance.DLog("Found file to process: " + file);
                }
            }
            Logger.Instance.DLog("Finished scanning libraries");
            CheckQueue();
        }

        private void CheckQueue()
        {
            if (FlowWorker.Processing == false && Queue.Any())
            {
                var libController = new Controllers.LibraryController();

                while (Queue.Any())
                {
                    var item = Queue.First();
                    var library = libController.Get(item.Key);
                    if (library == null)
                    {
                        Queue.Remove(item.Key);
                        continue;
                    }
                    while (item.Value.Any())
                    {
                        if (File.Exists(item.Value.First()) == false)
                        {
                            item.Value.RemoveAt(0);
                            continue;
                        }
                        Logger.Instance.DLog("About to request processing of: " + item.Value[0]);
                        FlowWorker.Process(library, item.Value[0]);
                        return;
                    }
                }
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