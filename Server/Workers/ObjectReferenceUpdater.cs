using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will update all object references if names change
/// </summary>
public class ObjectReferenceUpdater:Worker
{
    private static bool IsRunning = false;
    /// <summary>
    /// Creates a new instance of the Object Reference Updater 
    /// </summary>
    public ObjectReferenceUpdater() : base(ScheduleType.Daily, 1)
    {
    }

    protected override void Execute()
    {
        Run();
    }

    /// <summary>
    /// Runs the updater
    /// </summary>
    internal void Run()
    {
        if (IsRunning)
            return;
        IsRunning = true;
        try
        {
            var libFileController = new LibraryFileController();
            var libraryController = new LibraryController();
            DateTime start = DateTime.Now;
            var libFiles = libFileController.GetAll(null).Result;
            var libraries = libraryController.GetAll().Result;
            var flows = new FlowController().GetAll().Result;

            var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x.Name);
            var dictFlows = flows.ToDictionary(x => x.Uid, x => x.Name);
            
            Logger.Instance.ILog("Time Taken to prepare for ObjectReference rename: "+ DateTime.Now.Subtract(start));

            foreach (var lf in libFiles)
            {
                bool changed = false;
                if (dictLibraries.ContainsKey(lf.Library.Uid) && lf.Library.Name != dictLibraries[lf.Library.Uid])
                {
                    string oldName = lf.Library.Name;
                    lf.Library.Name = dictLibraries[lf.Library.Uid];
                    Logger.Instance.ILog($"Updating Library name reference '{oldName}' to'{lf.Library.Name}' in file: {lf.Name}");
                    changed = true;
                }

                if (lf.Flow != null && lf.Flow.Uid != Guid.Empty && dictFlows.ContainsKey(lf.Flow.Uid) &&
                    lf.Flow.Name != dictFlows[lf.Flow.Uid])
                {
                    string oldname = lf.Flow.Name;
                    lf.Flow.Name = dictFlows[lf.Flow.Uid];
                    Logger.Instance.ILog($"Updating Flow name reference '{oldname}' to '{lf.Flow.Name}' in file: {lf.Name}");
                    changed = true;
                }

                if (changed)
                    libFileController.Update(lf).Wait();
            }

            foreach (var lib in libraries)
            {
                if (dictFlows.ContainsKey(lib.Flow.Uid) && lib.Flow.Name != dictFlows[lib.Flow.Uid])
                {
                    string oldname = lib.Flow.Name;
                    lib.Flow.Name = dictFlows[lib.Flow.Uid];
                    Logger.Instance.ILog($"Updating Flow name reference '{oldname}' to '{lib.Flow.Name}' in library: {lib.Name}");
                    libraryController.Update(lib).Wait();
                }
            }
            Logger.Instance.ILog("Time Taken to complete for ObjectReference rename: "+ DateTime.Now.Subtract(start));
        }
        finally
        {
            IsRunning = false;
        }
    }
}