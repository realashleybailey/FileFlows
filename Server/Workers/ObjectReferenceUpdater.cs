using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will update all object references if names change
/// </summary>
public class ObjectReferenceUpdater:Worker
{
    
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
        var libFileController = new LibraryFileController();
        var libraryController = new LibraryController();
        var libFiles = libFileController.GetAll(null).Result;
        var libraries = libraryController.GetAll().Result;
        var flows = new FlowController().GetAll().Result;

        var dictLibraries = libraries.ToDictionary(x => x.Uid, x => x.Name);
        var dictFlows = flows.ToDictionary(x => x.Uid, x => x.Name);

        foreach (var lf in libFiles)
        {
            bool changed = false;
            if (dictLibraries.ContainsKey(lf.Library.Uid) && lf.Library.Name != dictLibraries[lf.Library.Uid])
            {
                lf.Library.Name = dictLibraries[lf.Library.Uid];
                Logger.Instance.ILog($"Updating Library name reference '{lf.Flow.Name}' in file: {lf.Name}");
                changed = true;
            }
            if (lf.Flow != null && lf.Flow.Uid != Guid.Empty && dictFlows.ContainsKey(lf.Flow.Uid) && lf.Flow.Name != dictFlows[lf.Flow.Uid])
            {
                lf.Flow.Name = dictFlows[lf.Flow.Uid];
                Logger.Instance.ILog($"Updating Flow name reference '{lf.Flow.Name}' in file: {lf.Name}");
                changed = true;
            }

            if (changed)
                libFileController.Update(lf).Wait();
        }

        foreach (var lib in libraries)
        {
            if (dictFlows.ContainsKey(lib.Flow.Uid) && lib.Flow.Name != dictFlows[lib.Flow.Uid])
            {
                lib.Flow.Name = dictFlows[lib.Flow.Uid];
                Logger.Instance.ILog($"Updating Flow name reference '{lib.Flow.Name}' in library: {lib.Name}");
                libraryController.Update(lib).Wait();
            }
        }
    }
}