using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Plugin;
using FileFlows.Server.Helpers.ModelHelpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Formatters;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library files controller
/// </summary>
[Route("/api/library-file")]
public class LibraryFileController : Controller //ControllerStore<LibraryFile>
{

    private static CacheStore CacheStore = new();

    /// <summary>
    /// Gets the next library file for processing, and puts it into progress
    /// </summary>
    /// <param name="args">The arguments for the call</param>
    /// <returns>the next library file to process</returns>
    [HttpPost("next-file")]
    public async Task<NextLibraryFileResult> GetNext([FromBody] NextLibraryFileArgs args)
    {
        var result = await new LibraryFileService().GetNext(args.NodeName, args.NodeUid, args.NodeVersion, args.WorkerUid);
        if (result == null)
            return result;
        Logger.Instance.ILog($"GetNextFile for ['{args.NodeName}']({args.NodeUid}): {result.Status}");
        return result;
    }

    /// <summary>
    /// Lists all of the library files, only intended for the UI
    /// </summary>
    /// <param name="status">The status to list</param>
    /// <param name="page">The page to get</param>
    /// <param name="pageSize">The number of items to fetch</param>
    /// <returns>a slimmed down list of files with only needed information</returns>
    [HttpGet("list-all")]
    public async Task<LibraryFileDatalistModel> ListAll([FromQuery] FileStatus status, [FromQuery] int page = 0, [FromQuery] int pageSize = 0)
    {
        var service = new LibraryFileService();
        var taskStatus = service.GetStatus();
        var taskLibraries = DbHelper.Select<Library>();
        var taskFiles = service.GetAll(status, page * pageSize, pageSize);
        await Task.WhenAll(taskStatus, taskLibraries, taskFiles);
        return new()
        {
            Status = taskStatus.Result,
            LibraryFiles = LibaryFileListModelHelper.ConvertToListModel(taskFiles.Result, status, taskLibraries.Result)
        };
            
        // if (DbHelper.UseMemoryCache == false)
        // {
        //     var taskOverview = DbHelper.GetLibraryFileOverview();
        //     var taskFiles = DbHelper.GetLibraryFiles(status, start: pageSize * page, max: pageSize);
        //     var taskLibraries = DbHelper.Select<Library>();
        //     Task.WaitAll(taskOverview, taskFiles, taskLibraries);
        //
        //     return new()
        //     {
        //         Status = taskOverview.Result,
        //         LibraryFiles = ConvertToListModel(taskFiles.Result, status, taskLibraries.Result)
        //     };
        // }
        
        
        //var allData  = await GetAllComplete(status);
        
        // var result = new LibraryFileDatalistModel();
        // result.Status = GetStatusData(allData.all, allData.libraries);
        // var libraries = await new LibraryController().GetAll();
        // result.LibraryFiles = ConvertToListModel(allData.results, status, libraries);
        //
        //
        // if (pageSize > 0)
        // {
        //     int startIndex = page * pageSize;
        //     var libaryFileListModels = result.LibraryFiles.ToList();
        //     if (libaryFileListModels.Count() < startIndex)
        //         result.LibraryFiles = new LibaryFileListModel[] { };
        //     else
        //         result.LibraryFiles = libaryFileListModels.Skip(startIndex).Take(pageSize);
        // }
        //
        // return result;
    }

    /// <summary>
    /// Gets all library files in the system
    /// </summary>
    /// <param name="status">The status to get, if missing will return all library files</param>
    /// <param name="skip">The amount of items to skip</param>
    /// <param name="top">The amount of items to grab, 0 to grab all</param>
    /// <returns>A list of library files</returns>
    [HttpGet]
    public async Task<IEnumerable<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0,
        [FromQuery] int top = 0)
    {
        //var result = await GetAllComplete(status, skip, top);
        //return result.results;
        return await new LibraryFileService().GetAll(status, skip, top);
    }
    
    
    // private async Task<(IEnumerable<LibraryFile> results, IEnumerable<LibraryFile> all, Dictionary<Guid, Library> libraries)> 
    //     GetAllComplete([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
    // {
    //     IEnumerable<LibraryFile> all = new LibraryFile[] { };
    //     IEnumerable<LibraryFile> libraryFiles = new LibraryFile[] { };
    //     Dictionary<Guid, Library> libraries = new Dictionary<Guid, Library>();
    //     
    //     await Task.WhenAll(new Task[]
    //     {
    //         Task.Run(async () => all = await base.GetDataList()),
    //         Task.Run(async () => libraries = await new LibraryController().GetData())
    //     });
    //
    //     libraryFiles = all;
    //     
    //     if (status != null && status != FileStatus.MissingLibrary)
    //     {
    //         FileStatus searchStatus =
    //             (status.Value == FileStatus.OutOfSchedule || status.Value == FileStatus.Disabled || status.Value == FileStatus.OnHold)
    //                 ? FileStatus.Unprocessed
    //                 : status.Value;
    //         libraryFiles = libraryFiles.Where(x => x.Status == searchStatus);
    //     }
    //     else if (status == FileStatus.MissingLibrary)
    //     {
    //         libraryFiles = libraryFiles.Where(x => libraries.ContainsKey(x.Library.Uid) == false);
    //     }
    //
    //
    //     if (status == FileStatus.Unprocessed || status == FileStatus.OutOfSchedule || status == FileStatus.OnHold)
    //     {
    //         var filteredResults = libraryFiles
    //             .Where(x =>
    //             {
    //                 // unprocessed just show the enabled libraries
    //                 if (x.Library == null || libraries.ContainsKey(x.Library.Uid) == false)
    //                     return false;
    //                 var lib = libraries[x.Library.Uid];
    //                 if (lib.Enabled == false)
    //                     return false;
    //                 if (TimeHelper.InSchedule(lib.Schedule) == false)
    //                     return status == FileStatus.OutOfSchedule;
    //                 if (lib.HoldMinutes != 0 && x.DateCreated > DateTime.Now.AddMinutes(-lib.HoldMinutes))                    
    //                     return status == FileStatus.OnHold;
    //                 return status == FileStatus.Unprocessed;
    //             })
    //             .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
    //             .ThenByDescending(x =>
    //             {
    //                 // check the processing priority of the library
    //                 if (x.Library != null && libraries.ContainsKey(x.Library.Uid))
    //                 {
    //                     return (int)libraries[x.Library.Uid].Priority;
    //                 }
    //
    //                 return (int)ProcessingPriority.Normal;
    //             })
    //             .ThenBy(x => x.DateCreated);
    //         return (filteredResults, all, libraries);
    //     }
    //
    //     if (status == FileStatus.Disabled)
    //     {
    //         var filteredResults = libraryFiles
    //             .Where(x =>
    //             {
    //                 // unprocessed just show the enabled libraries
    //                 if (x.Library == null || libraries.ContainsKey(x.Library.Uid) == false)
    //                     return false;
    //                 var lib = libraries[x.Library.Uid];
    //                 return lib.Enabled == false;
    //             });
    //         return (filteredResults, all, libraries);
    //     }
    //
    //     if (status == FileStatus.Processing)
    //         return (libraryFiles, all, libraries);;
    //
    //     IEnumerable<LibraryFile> results = libraryFiles.OrderByDescending(x => x.ProcessingEnded);
    //
    //     if (skip > 0)
    //         results = results.Skip(skip);
    //     if (top > 0)
    //         results = results.Take(top);
    //
    //
    //     return (results, all, libraries);
    // }

    // private async Task<IEnumerable<LibraryFile>> OrderLibraryFiles(IEnumerable<LibraryFile> libraryFiles, FileStatus status)
    // {
    //     Dictionary<Guid, Library> libraries = await new LibraryController().GetData();
    //     
    //     if (status is FileStatus.Unprocessed or FileStatus.OutOfSchedule)
    //     {
    //         return libraryFiles
    //             .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
    //             .ThenByDescending(x =>
    //             {
    //                 // check the processing priority of the library
    //                 if (x.Library != null && libraries.ContainsKey(x.Library.Uid))
    //                 {
    //                     return (int)libraries[x.Library.Uid].Priority;
    //                 }
    //
    //                 return (int)ProcessingPriority.Normal;
    //             })
    //             .ThenBy(x => x.DateCreated);
    //     }
    //
    //     if (status == FileStatus.Processed)
    //         return libraryFiles.OrderByDescending(x => x.ProcessingEnded);
    //
    //     return libraryFiles;
    // }

    /// <summary>
    /// Get next 10 upcoming files to process
    /// </summary>
    /// <returns>a list of upcoming files to process</returns>
    [HttpGet("upcoming")]
    public Task<IEnumerable<LibraryFile>> Upcoming()
        => new LibraryFileService().GetAll(FileStatus.Unprocessed, rows: 10);
        // if (DbHelper.UseMemoryCache)
        // {
        //     var libFiles = await GetAll(FileStatus.Unprocessed);
        //     return libFiles.Take(10);
        // }
        //
        // return await DbHelper.GetLibraryFiles(FileStatus.Unprocessed, max: 10);
    

    /// <summary>
    /// Gets the last 10 successfully processed files
    /// </summary>
    /// <returns>the last successfully processed files</returns>
    [HttpGet("recently-finished")]
    public Task<IEnumerable<LibraryFile>> RecentlyFinished() 
        => new LibraryFileService().GetAll(FileStatus.Processed, rows: 10);
        
        // if (DbHelper.UseMemoryCache)
        // {
        //     var libraryFiles = await GetDataList();
        //     return libraryFiles
        //         .Where(x => x.Status == FileStatus.Processed)
        //         .OrderByDescending(x => x.ProcessingEnded)
        //         .Take(10);
        // }
        //
        // return await DbHelper.GetLibraryFiles(FileStatus.Processed, max: 10);
    

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("status")]
    public Task<IEnumerable<LibraryStatus>> GetStatus()
        => new LibraryFileService().GetStatus();
    
    // private IEnumerable<LibraryStatus> GetStatusData(IEnumerable<LibraryFile> libraryFiles, IDictionary<Guid, Library> libraries)
    // {
    //     var statuses = libraryFiles.Select(x =>
    //     {
    //         if (x.Status != FileStatus.Unprocessed)
    //             return x.Status;
    //         // unprocessed just show the enabled libraries
    //         if (libraries.ContainsKey(x.Library.Uid) == false)
    //             return FileStatus.MissingLibrary;
    //
    //         var lib = libraries[x.Library.Uid];
    //         if (lib.Enabled == false)
    //             return FileStatus.Disabled;
    //         if (TimeHelper.InSchedule(lib.Schedule) == false)
    //             return FileStatus.OutOfSchedule;
    //         if (lib.HoldMinutes != 0 && x.DateCreated > DateTime.Now.AddMinutes(-lib.HoldMinutes))
    //             return FileStatus.OnHold;
    //         return FileStatus.Unprocessed;
    //     });
    //
    //     return statuses.GroupBy(x => x)
    //         .Select(x => new LibraryStatus { Status = x.Key, Count = x.Count() });
    //
    // }


    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile> Get(Guid uid)
    {
        var result = await new LibraryFileService().Get(uid);
        // if(DbHelper.UseMemoryCache == false && result != null)
        //     CacheStore.Store(result.Uid, result);
        if(result != null && (result.Status == FileStatus.ProcessingFailed || result.Status == FileStatus.Processed))
        {
            if (LibraryFileLogHelper.HtmlLogExists(uid))
                return result;
            LibraryFileLogHelper.CreateHtmlOfLog(uid);
        }
        return result;
    }


    /// <summary>
    /// Update a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The updated library file</returns>
    [HttpPut]
    public async Task<LibraryFile> Update([FromBody] LibraryFile file)
    {
        var existing = await new LibraryFileService().Get(file.Uid);

        if (existing == null)
            throw new Exception("Not found");

        if (existing.Status == FileStatus.Processed && file.Status == FileStatus.Processing)
        {
            // already finished and reported finished in the Worker controller.  So ignore this out of date update
            return existing;
        }

        if (existing.Status != file.Status)
        {
            Logger.Instance?.ILog($"Setting library file status to: {file.Status} - {file.Name}");
            existing.Status = file.Status;
        }

        existing.Node = file.Node;
        if(existing.FinalSize == 0 || file.FinalSize > 0)
            existing.FinalSize = file.FinalSize;
        if(file.OriginalSize > 0)
            existing.OriginalSize = file.OriginalSize;
        if(string.IsNullOrEmpty(file.OutputPath))
            existing.OutputPath = file.OutputPath;
        existing.Flow = file.Flow;
        if(file.Library != null && file.Library.Uid == existing.Library.Uid)
            existing.Library = file.Library; // name may have changed and is being updated
        
        existing.DateCreated = file.DateCreated; // this can be changed if library file is unheld
        existing.ProcessingEnded = file.ProcessingEnded;
        existing.ProcessingStarted = file.ProcessingStarted;
        existing.WorkerUid = file.WorkerUid;
        existing.ExecutedNodes = file.ExecutedNodes ?? new List<ExecutedNode>();
        if (file.OriginalMetadata?.Any() == true)
            existing.OriginalMetadata = file.OriginalMetadata;
        if (file.FinalMetadata?.Any() == true)
            existing.FinalMetadata = file.FinalMetadata;
        
        var updated = await new LibraryFileService().Update(file);
        
        // if(DbHelper.UseMemoryCache == false)
        //     CacheStore.Store(updated.Uid, updated);
        
        return updated;
    }


    /// <summary>
    /// Downloads a  log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The download action result</returns>
    [HttpGet("{uid}/log/download")]
    public IActionResult GetLog([FromRoute] Guid uid)
    {     
        string log = LibraryFileLogHelper.GetLog(uid);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
        return File(data, "application/octet-stream", uid + ".log");
    }
    
    /// <summary>
    /// Get the log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lines">Optional number of lines to fetch</param>
    /// <param name="html">if the log should be html if possible</param>
    /// <returns>The log of the library file</returns>
    [HttpGet("{uid}/log")]
    public string GetLog([FromRoute] Guid uid, [FromQuery] int lines = 0, [FromQuery] bool html = true)
    {
        try
        {
            return html ? LibraryFileLogHelper.GetHtmlLog(uid, lines) : LibraryFileLogHelper.GetLog(uid);
        }
        catch (Exception ex)
        {
            return "Error opening log: " + ex.Message;
        }
    }

    /// <summary>
    /// Saves the full log for a library file
    /// Call this after processing has completed for a library file
    /// </summary>
    /// <param name="uid">The uid of the library file</param>
    /// <param name="log">the log</param>
    /// <returns>true if successfully saved log</returns>
    [HttpPut("{uid}/full-log")]
    public async Task<bool> SaveFullLog([FromRoute] Guid uid, [FromBody] string log)
    {
        try
        {
            await LibraryFileLogHelper.SaveLog(uid, log, saveHtml: true);
            return true;
        }
        catch (Exception) { }
        return false;
    }

    /// <summary>
    /// A reference model of library files to move to the top of the processing queue
    /// </summary>
    /// <param name="model">The reference model of items in order to move</param>
    /// <returns>an awaited task</returns>
    [HttpPost("move-to-top")]
    public async Task MoveToTop([FromBody] ReferenceModel<Guid> model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete

        var list = model.Uids.ToArray();
        await new LibraryFileService().MoveToTop(list);
    }

    internal Task<LibraryFile> Add(LibraryFile libraryFile) =>
        new LibraryFileService().Add(libraryFile);

    internal Task AddMany(LibraryFile[] libraryFiles)
        => new LibraryFileService().AddMany(libraryFiles);

    /// <summary>
    /// Checks if a library file exists on the server
    /// </summary>
    /// <param name="uid">The Uid of the library file to check</param>
    /// <returns>true if exists, otherwise false</returns>
    [HttpGet("exists-on-server/{uid}")]
    public async Task<bool> ExistsOnServer([FromRoute] Guid uid)
    {
        var libFile = await Get(uid);
        if (libFile == null)
            return false;
        try
        {
            return System.IO.File.Exists(libFile.Name);
        }
        catch (Exception) { return false; }
    }

    /// <summary>
    /// Delete library files from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Delete(model?.Uids);

    /// <summary>
    /// Reprocess library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocess</param>
    /// <returns>an awaited task</returns>
    [HttpPost("reprocess")]
    public Task Reprocess([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Reprocess(model.Uids);

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocess</param>
    /// <returns>an awaited task</returns>
    [HttpPost("unhold")]
    public Task Unhold([FromBody] ReferenceModel<Guid> model)
        => new LibraryFileService().Unhold(model.Uids);
    

    /// <summary>
    /// Gets the shrinkage data for a bar chart
    /// </summary>
    /// <returns>the bar chart data</returns>
    [HttpGet("shrinkage-bar-chart")]
    public async Task<object> ShrinkageBarChart()
    {
        var groups = await ShrinkageGroups();
        #if(DEBUG)
        groups = new Dictionary<string, ShrinkageData>()
        {
            { "Movies", new() { FinalSize = 10_000_000_000, OriginalSize = 25_000_000_000 } },
            { "TV", new() { FinalSize = 45_000_000_000, OriginalSize = 75_000_000_000 } },
            { "Other", new() { FinalSize = 45_000_000_000, OriginalSize = 40_000_000_000 } },
            { "Other2", new() { FinalSize = 15_000_000_000, OriginalSize = 20_000_000_000 } },
            { "Other3", new() { FinalSize = 27_000_000_000, OriginalSize = 30_000_000_000 } },
        };
        #endif
        return new
        {
            series = new object[]
            {
                //new { name = "Final Size", data = groups.Select(x => (x.Value.OriginalSize - x.Value.FinalSize)).ToArray() },
                new { name = "Final Size", data = groups.Select(x => x.Value.FinalSize).ToArray() },
                //new { name = "Original Size", data = groups.Select(x => x.Value.OriginalSize).ToArray() }
                new { name = "Savings", data = groups.Select(x =>
                {
                    var change = x.Value.OriginalSize - x.Value.FinalSize;
                    if (change > 0)
                        return change;
                    return 0;
                }).ToArray() },
                new { name = "Increase", data = groups.Select(x =>
                {
                    var change = x.Value.OriginalSize - x.Value.FinalSize;
                    if (change > 0)
                        return 0;
                    return change * -1;
                }).ToArray() }
            },
            labels = groups.Select(x => x.Key.Replace("###TOTAL###", "Total")).ToArray(),
        };
    }

    /// <summary>
    /// Get library file shrinkage grouped by library
    /// </summary>
    /// <returns>the library file shrinkage data</returns>
    [HttpGet("shrinkage-groups")]
    public async Task<Dictionary<string, ShrinkageData>> ShrinkageGroups()
    {
        var data = await new LibraryFileService().GetShrinkageGroups();
        var libraries = data.ToDictionary(x => x.Library, x => x);
        ShrinkageData total = new ShrinkageData();
        foreach (var lib in libraries)
        {
            total.FinalSize += lib.Value.FinalSize;
            total.OriginalSize += lib.Value.OriginalSize;
            total.Items += lib.Value.Items;
        }
        
        if (libraries.Count > 5)
        {
            ShrinkageData other = new ShrinkageData();
            while (libraries.Count > 4)
            {
                List<string> toRemove = new();
                var sd = libraries.OrderBy(x => x.Value.Items).First();
                other.Items += sd.Value.Items;
                other.FinalSize += sd.Value.FinalSize;
                other.OriginalSize += sd.Value.OriginalSize;
                libraries.Remove(sd.Key);
            }

            libraries.Add("###OTHER###", other);
        }
        
        if (libraries.ContainsKey("###TOTAL###") ==
            false) // so unlikely, only if they named a library this, but just incase they did
            libraries.Add("###TOTAL###", total);
        return libraries;
    }

    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    [HttpPost("search")]
    public Task<IEnumerable<LibraryFile>> Search([FromBody] LibraryFileSearchModel filter)
        => new LibraryFileService().Search(filter);


    /// <summary>
    /// Get a specific library file using cache
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    internal Task<LibraryFile> GetCached(Guid uid)
        => new LibraryFileService().Get(uid);
    //
    // {
    //
    //     throw new NotImplementedException();
    //     // if(DbHelper.UseMemoryCache)
    //     //     return await GetByUid(uid);
    //     //
    //     // // using mysql, a little more complicated
    //     // var cached = CacheStore.Get<LibraryFile>(uid);
    //     // if (cached == null)
    //     // {
    //     //     cached = await GetByUid(uid);
    //     //     CacheStore.Store(uid, cached);
    //     // }
    //     // return cached;
    // }
}
