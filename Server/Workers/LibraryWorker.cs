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

            bool scannedAny = false;
            foreach(var libwatcher in WatchedLibraries.Values)
            {
                var library = libraries.Where(x => libwatcher.Library.Uid == x.Uid).FirstOrDefault();
                if (library != null)
                    libwatcher.UpdateLibrary(library);
                scannedAny |= libwatcher.Scan();
            }
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
    }
}