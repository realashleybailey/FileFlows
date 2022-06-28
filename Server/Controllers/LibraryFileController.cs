using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Plugin;
using FileFlows.Shared.Formatters;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Library files controller
/// </summary>
[Route("/api/library-file")]
public class LibraryFileController : ControllerStore<LibraryFile>
{

    /// <summary>
    /// Gets the next library file for processing, and puts it into progress
    /// </summary>
    /// <param name="args">The arguments for the call</param>
    /// <returns>the next library file to process</returns>
    [HttpPost("next-file")]
    public async Task<NextLibraryFileResult> GetNext([FromBody] NextLibraryFileArgs args)
    {
        var result = await GetNextActual(args);
        if (result == null)
            return result;
        Logger.Instance.ILog($"GetNextFile for ['{args.NodeName}']({args.NodeUid}): {result.Status}");
        return result;
    }
    
    
    async Task<NextLibraryFileResult> GetNextActual([FromBody] NextLibraryFileArgs args)
    {
        _ = new NodeController().UpdateLastSeen(args.NodeUid);
        
        if (Workers.ServerUpdater.UpdatePending || args == null)
            return NextFileResult (NextLibraryFileStatus.UpdatePending); // if an update is pending, stop providing new files to process

        var settings = await new SettingsController().Get();
        if (settings.IsPaused)
            return NextFileResult(NextLibraryFileStatus.SystemPaused);

        if (Version.TryParse(args.NodeVersion, out var nodeVersion) == false)
            return NextFileResult(NextLibraryFileStatus.InvalidVersion);

        if (nodeVersion < Globals.MinimumNodeVersion)
        {
            Logger.Instance.ILog($"Node '{args.NodeName}' version '{nodeVersion}' is less than minimum supported version '{Globals.MinimumNodeVersion}'");
            return NextFileResult(NextLibraryFileStatus.VersionMismatch);
        }

        var node = (await new NodeController().Get(args.NodeUid));
        if (node != null && node.Version != args.NodeVersion)
        {
            node.Version = args.NodeVersion;
            await new NodeController().Update(node);
        }

        if (await NodeEnabled(node) == false)
            return NextFileResult(NextLibraryFileStatus.NodeNotEnabled);

        if (DbHelper.UseMemoryCache)
            return await GetNextMemoryCache(node, args.WorkerUid);
        return await GetNextDb(node, args.WorkerUid);

    }

    private async Task<bool> NodeEnabled(ProcessingNode node)
    {
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var allNodes = await new NodeController().GetAll();
        var enabledNodes = allNodes.Where(x => x.Enabled).OrderBy(x => x.Name).Take(licensedNodes).ToArray();
        var enabledNodeUids = enabledNodes.Select(x => x.Uid).ToArray();
        return enabledNodeUids.Contains(node.Uid);
    }

    private async Task<NextLibraryFileResult> GetNextDb(ProcessingNode node, Guid workerUid)
    {
        await _mutex.WaitAsync();
        try
        {
            var item = (await DbHelper.GetLibraryFiles(FileStatus.Unprocessed, start: 0, max: 1, nodeUid: node.Uid))
                .FirstOrDefault();
            if (item == null)
                return NextFileResult(NextLibraryFileStatus.NoFile, null);
            item.Status = FileStatus.Processing;
            item.Node = new ObjectReference { Uid = node.Uid, Name = node.Name };
            item.WorkerUid = workerUid;
            item.ProcessingStarted = DateTime.Now;
            await DbHelper.Update(item);
            return NextFileResult(NextLibraryFileStatus.Success, item);
        }
        catch(Exception ex)
        {
            Logger.Instance.ELog("Error Getting Next File From DB: " + ex.Message);
            throw;
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>
    /// Constructs a next library file result
    /// <param name="status">the status of the call</param>
    /// <param name="file">the library file to process</param>
    /// </summary>
    private NextLibraryFileResult NextFileResult(NextLibraryFileStatus? status = null, LibraryFile file = null)
    {
        NextLibraryFileResult result = new();
        if (status != null)
            result.Status = status.Value;
        result.File = file;
        return result;
    }
    
    private async Task<NextLibraryFileResult> GetNextMemoryCache(ProcessingNode node, Guid workerUid)
    {
        var data = (await GetAll(FileStatus.Unprocessed)).ToArray();
        await _mutex.WaitAsync();
        try
        {
            // iterate these in case, something starts processing
            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item.Status != FileStatus.Unprocessed)
                    continue;

                string nodeName = node.Name == "FileFlowsServer" ? "Internal Processing Node" : node.Name;

                if (node.AllLibraries == ProcessingLibraries.Only)
                {
                    if (node.Libraries?.Any(x => x.Uid == item.Library?.Uid) != true)
                    {
                        Logger.Instance?.DLog($"Library '{(item.Library?.Name ?? "UNKNOWN")}' not available for node '{nodeName}': " + item.Name);
                        continue;
                    }
                }
                else if (node.AllLibraries == ProcessingLibraries.AllExcept)
                {
                    if (node.Libraries?.Any(x => x.Uid == item.Library?.Uid) == true)
                    {
                        Logger.Instance?.DLog($"Library '{(item.Library?.Name ?? "UNKNOWN")}' not available for node '{nodeName}': " + item.Name);
                        continue;
                    }
                }

                if (node.MaxFileSizeMb > 0)
                {
                    if (item.OriginalSize > (node.MaxFileSizeMb * 1000L * 1000L))
                    {
                        var nodeLimit = FileSizeFormatter.Format(node.MaxFileSizeMb * 1000L * 1000L);
                        Logger.Instance?.DLog($"File size '{FileSizeFormatter.Format(item.OriginalSize)} is over file size for node '{nodeName}'({nodeLimit}): " + item.Name);
                        continue;
                    }
                }

                item.Status = FileStatus.Processing;
                item.Node = new ObjectReference { Uid = node.Uid, Name = node.Name };
                item.WorkerUid = workerUid;
                item.ProcessingStarted = DateTime.Now;
                data[i] = await DbHelper.Update(item);
                return NextFileResult (NextLibraryFileStatus.Success, data[i]);
            }
            return NextFileResult (NextLibraryFileStatus.NoFile, null);
        }
        finally
        {
            _mutex.Release();
        }
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
        if (DbHelper.UseMemoryCache == false)
        {
            var taskOverview = DbHelper.GetLibraryFileOverview();
            var taskFiles = DbHelper.GetLibraryFiles(status, start: pageSize * page, max: pageSize);
            Task.WaitAll(taskOverview, taskFiles);

            return new()
            {
                Status = taskOverview.Result,
                LibraryFiles = ConvertToListModel(taskFiles.Result, status)
            };
        }
        
        
        var allData  = await GetAllComplete(status);
        
        var result = new LibraryFileDatalistModel();
        result.Status = GetStatusData(allData.all, allData.libraries);
        result.LibraryFiles = ConvertToListModel(allData.results, status);


        if (pageSize > 0)
        {
            int startIndex = page * pageSize;
            var libaryFileListModels = result.LibraryFiles.ToList();
            if (libaryFileListModels.Count() < startIndex)
                result.LibraryFiles = new LibaryFileListModel[] { };
            else
                result.LibraryFiles = libaryFileListModels.Skip(startIndex).Take(pageSize);
        }

        return result;
    }

    private IEnumerable<LibaryFileListModel> ConvertToListModel(IEnumerable<LibraryFile> files, FileStatus status)
    {
        files = files.ToList();
        return files.Select(x =>
        {
            var item = new LibaryFileListModel
            {
                Uid = x.Uid,
                Flow = x.Flow?.Name,
                Library = x.Library?.Name,
                RelativePath = x.RelativePath,
                Name = x.Name
            };

            if (status == FileStatus.Unprocessed || status == FileStatus.OutOfSchedule || status == FileStatus.Disabled)
            {
                item.Date = x.DateCreated;
            }

            if (status == FileStatus.Processing)
            {
                item.Node = x.Node?.Name;
                item.ProcessingTime = x.ProcessingTime;
                item.Date = x.ProcessingStarted;
            }

            if (status == FileStatus.ProcessingFailed)
            {
                item.Date = x.ProcessingEnded;
            }

            if (status == FileStatus.Duplicate)
                item.Duplicate = x.Duplicate?.Name;

            if (status == FileStatus.MissingLibrary)
                item.Status = x.Status;

            if (status == FileStatus.Processed)
            {
                item.FinalSize = x.FinalSize;
                item.OriginalSize = x.OriginalSize;
                item.OutputPath = x.OutputPath;
                item.ProcessingTime = x.ProcessingTime;
                item.Date = x.ProcessingEnded;
            }
            return item;
        });
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
        var result = await GetAllComplete(status, skip, top);
        return result.results;
    }
    
    
    private async Task<(IEnumerable<LibraryFile> results, IEnumerable<LibraryFile> all, Dictionary<Guid, Library> libraries)> 
        GetAllComplete([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
    {
        IEnumerable<LibraryFile> all = new LibraryFile[] { };
        IEnumerable<LibraryFile> libraryFiles = new LibraryFile[] { };
        Dictionary<Guid, Library> libraries = new Dictionary<Guid, Library>();
        
        await Task.WhenAll(new Task[]
        {
            Task.Run(async () => all = await base.GetDataList()),
            Task.Run(async () => libraries = await new LibraryController().GetData())
        });

        libraryFiles = all;
        
        if (status != null && status != FileStatus.MissingLibrary)
        {
            FileStatus searchStatus =
                (status.Value == FileStatus.OutOfSchedule || status.Value == FileStatus.Disabled)
                    ? FileStatus.Unprocessed
                    : status.Value;
            libraryFiles = libraryFiles.Where(x => x.Status == searchStatus);
        }


        if (status == FileStatus.Unprocessed || status == FileStatus.OutOfSchedule)
        {
            var filteredResults = libraryFiles
                .Where(x =>
                {
                    // unprocessed just show the enabled libraries
                    if (x.Library == null || libraries.ContainsKey(x.Library.Uid) == false)
                        return false;
                    var lib = libraries[x.Library.Uid];
                    if (lib.Enabled == false)
                        return false;
                    if (TimeHelper.InSchedule(lib.Schedule) == false)
                        return status == FileStatus.OutOfSchedule;
                    return status == FileStatus.Unprocessed;
                })
                .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                .ThenByDescending(x =>
                {
                    // check the processing priority of the library
                    if (x.Library != null && libraries.ContainsKey(x.Library.Uid))
                    {
                        return (int)libraries[x.Library.Uid].Priority;
                    }

                    return (int)ProcessingPriority.Normal;
                })
                .ThenBy(x => x.DateCreated);
            return (filteredResults, all, libraries);
        }

        if (status == FileStatus.Disabled)
        {
            var filteredResults = libraryFiles
                .Where(x =>
                {
                    // unprocessed just show the enabled libraries
                    if (x.Library == null || libraries.ContainsKey(x.Library.Uid) == false)
                        return false;
                    var lib = libraries[x.Library.Uid];
                    return lib.Enabled == false;
                });
            return (filteredResults, all, libraries);
        }

        if (status == FileStatus.Processing)
            return (libraryFiles, all, libraries);;

        IEnumerable<LibraryFile> results = libraryFiles.OrderByDescending(x => x.ProcessingEnded);

        if (skip > 0)
            results = results.Skip(skip);
        if (top > 0)
            results = results.Take(top);


        return (results, all, libraries);
    }

    private async Task<IEnumerable<LibraryFile>> OrderLibraryFiles(IEnumerable<LibraryFile> libraryFiles, FileStatus status)
    {
        Dictionary<Guid, Library> libraries = await new LibraryController().GetData();
        
        if (status is FileStatus.Unprocessed or FileStatus.OutOfSchedule)
        {
            return libraryFiles
                .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                .ThenByDescending(x =>
                {
                    // check the processing priority of the library
                    if (x.Library != null && libraries.ContainsKey(x.Library.Uid))
                    {
                        return (int)libraries[x.Library.Uid].Priority;
                    }

                    return (int)ProcessingPriority.Normal;
                })
                .ThenBy(x => x.DateCreated);
        }

        if (status == FileStatus.Processed)
            return libraryFiles.OrderByDescending(x => x.ProcessingEnded);

        return libraryFiles;
    }

    /// <summary>
    /// Get next 10 upcoming files to process
    /// </summary>
    /// <returns>a list of upcoming files to process</returns>
    [HttpGet("upcoming")]
    public async Task<IEnumerable<LibraryFile>> Upcoming()
    {
        if (DbHelper.UseMemoryCache)
        {
            var libFiles = await GetAll(FileStatus.Unprocessed);
            return libFiles.Take(10);
        }

        return await DbHelper.GetLibraryFiles(FileStatus.Unprocessed, max: 10);
    }

    /// <summary>
    /// Gets the last 10 successfully processed files
    /// </summary>
    /// <returns>the last successfully processed files</returns>
    [HttpGet("recently-finished")]
    public async Task<IEnumerable<LibraryFile>> RecentlyFinished()
    {
        if (DbHelper.UseMemoryCache)
        {
            var libraryFiles = await GetDataList();
            return libraryFiles
                .Where(x => x.Status == FileStatus.Processed)
                .OrderByDescending(x => x.ProcessingEnded)
                .Take(10);
        }

        return await DbHelper.GetLibraryFiles(FileStatus.Processed, max: 10);
    }

    internal async Task ResetProcessingStatus(Guid nodeUid)
    {
        var libfiles = await GetDataList();
        var uids = libfiles.Where(x => x.Status == FileStatus.Processing && x.Node?.Uid == nodeUid).Select(x => x.Uid).ToArray();
        if (uids.Any())
            await Reprocess(new ReferenceModel { Uids = uids });
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("status")]
    public async Task<IEnumerable<LibraryStatus>> GetStatus()
    {
        var libraryFiles = await GetDataList();
        var libraries = await new LibraryController().GetData();
        return GetStatusData(libraryFiles, libraries);
    }
    
    private IEnumerable<LibraryStatus> GetStatusData(IEnumerable<LibraryFile> libraryFiles, IDictionary<Guid, Library> libraries)
    {
        var statuses = libraryFiles.Select(x =>
        {
            if (x.Status != FileStatus.Unprocessed)
                return x.Status;
            // unprocessed just show the enabled libraries
            if (libraries.ContainsKey(x.Library.Uid) == false)
                return FileStatus.MissingLibrary;

            var lib = libraries[x.Library.Uid];
            if (lib.Enabled == false)
                return FileStatus.Disabled;
            if (TimeHelper.InSchedule(lib.Schedule) == false)
                return FileStatus.OutOfSchedule;
            return FileStatus.Unprocessed;
        });

        return statuses.GroupBy(x => x)
            .Select(x => new LibraryStatus { Status = x.Key, Count = x.Count() });

    }


    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile> Get(Guid uid)
    {
        var result = await GetByUid(uid);
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
        var existing = await GetByUid(file.Uid);
        if (existing == null)
            throw new Exception("Not found");

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
        existing.ProcessingEnded = file.ProcessingEnded;
        existing.ProcessingStarted = file.ProcessingStarted;
        existing.WorkerUid = file.WorkerUid;
        existing.ExecutedNodes = file.ExecutedNodes ?? new List<ExecutedNode>();
        return await base.Update(existing);
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
    public async Task MoveToTop([FromBody] ReferenceModel model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete

        var list = model.Uids.ToList();

        // clear the list to make sure its upt to date
        var libraryFiles = await GetDataList();

        var libFiles = libraryFiles
                               .Where(x => x.Status == FileStatus.Unprocessed)
                               .OrderBy(x =>
                                {
                                    int index = list.IndexOf(x.Uid);
                                    if (index >= 0)
                                    {
                                        x.Order = index + 1;
                                        return index;
                                    }
                                    else if (x.Order > 0)
                                    {
                                        x.Order = list.Count + x.Order - 1;
                                        return x.Order;
                                    }
                                    return int.MaxValue;
                                })
                                .Where(x => x.Order > 0)
                                .ToList();
        int order = 0;
        foreach (var libFile in libFiles)
        {
            libFile.Order = ++order;
            await DbHelper.Update(libFile);
        }
    }

    internal Task<LibraryFile> Add(LibraryFile libraryFile) => base.Update(libraryFile);

    internal async Task AddMany(LibraryFile[] libraryFiles)
    {
        try
        {
            await DbHelper.AddMany(libraryFiles);
            if (DbHelper.UseMemoryCache)
            {
                if (_Data == null)
                    await GetData();

                lock (_Data)
                {
                    foreach (var lf in libraryFiles)
                        _Data.Add(lf.Uid, lf);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("LibraryFileController.AddMany: Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }
    }

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
    public async Task Delete([FromBody] ReferenceModel model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete
        await DeleteAll(model);
    }

    /// <summary>
    /// Reprocess library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to reprocess</param>
    /// <returns>an awaited task</returns>
    [HttpPost("reprocess")]
    public async Task Reprocess([FromBody] ReferenceModel model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete
        var list = model.Uids.ToList();

        // clear the list to make sure its upt to date
        var libraryFiles = await GetData();
        foreach (var uid in model.Uids)
        {
            LibraryFile item;
            lock (libraryFiles)
            {
                if (libraryFiles.ContainsKey(uid) == false)
                    continue;
                item = libraryFiles[uid];
                if (item.Status != FileStatus.ProcessingFailed && item.Status != FileStatus.Processed && item.Status != FileStatus.Duplicate && item.Status != FileStatus.MappingIssue)
                    continue;
                item.Status = FileStatus.Unprocessed;
            }
            await Update(item);
        }
    }

    /// <summary>
    /// Gets total shrinkage of all processed library files
    /// </summary>
    /// <returns>the total library file shrinkage</returns>
    [HttpGet("shrinkage")]
    public async Task<ShrinkageData> Shrinkage()
    {
        double original = 0;
        double final = 0;

        var files = await GetDataList();
        foreach(var file in files)
        {
            if (file.Status != FileStatus.Processed || file.OriginalSize == 0 || file.FinalSize == 0)
                continue;
            original += file.OriginalSize;
            final += file.FinalSize;
        }
        return new ShrinkageData
        {
            FinalSize = final,
            OriginalSize = original
        };
    }

    /// <summary>
    /// Get library file shrinkage grouped by library
    /// </summary>
    /// <returns>the library file shrinkage data</returns>
    [HttpGet("shrinkage-groups")]
    public async Task<Dictionary<string, ShrinkageData>> ShrinkageGroups()
    {
        var libraries = DbHelper.UseMemoryCache ? await ShrinkageGroupsInMemory() : await ShrinkageGroupsDb();
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
#if (DEBUG)
        if (libraries.Any() == false)
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            double min = 10_000_000;
            int count = 0;
            libraries = Enumerable.Range(1, 6).Select(x => new ShrinkageData
            {
                FinalSize = rand.NextDouble() * min + min,
                OriginalSize = rand.NextDouble() * min + min
            }).ToDictionary(x => "Library " + (++count), x => x);
            total.FinalSize = 0;
            total.OriginalSize = 0;
            foreach (var lib in libraries)
            {
                total.FinalSize += lib.Value.FinalSize;
                total.OriginalSize += lib.Value.OriginalSize;
            }
        }
#endif
        if (libraries.ContainsKey("###TOTAL###") ==
            false) // so unlikely, only if they named a library this, but just incase they did
            libraries.Add("###TOTAL###", total);
        return libraries;
    }

    /// <summary>
    /// Get ShrinkageGroup data from a stored procedure
    /// </summary>
    /// <returns>the shrinkage group date</returns>
    private Task<Dictionary<string, ShrinkageData>> ShrinkageGroupsDb() => DbHelper.GetShrinkageGroups();

    private async Task<Dictionary<string, ShrinkageData>> ShrinkageGroupsInMemory()
    {
        var files = await GetDataList();
        Dictionary<string, ShrinkageData> libraries = new();
        foreach (var file in files)
        {
            if (file.Status != FileStatus.Processed || file.OriginalSize == 0 || file.FinalSize == 0)
                continue;
            if (libraries.ContainsKey(file.Library.Name) == false)
            {
                libraries.Add(file.Library.Name, new ShrinkageData()
                {
                    FinalSize = file.FinalSize,
                    OriginalSize = file.OriginalSize
                });
            }
            else
            {

                libraries[file.Library.Name].OriginalSize += file.OriginalSize;
                libraries[file.Library.Name].FinalSize += file.FinalSize;
                libraries[file.Library.Name].Items++;
            }
        }

        return libraries;

    }

    internal async Task UpdateFlowName(Guid uid, string name)
    {
        var libraryFiles = await GetDataList();
        foreach (var lf in libraryFiles.Where(x => x.Flow?.Uid == uid))
        {
            lf.Flow.Name = name;
            await Update(lf);
        }
    }

    /// <summary>
    /// Deletes all the library files from the specified libraries
    /// </summary>
    /// <param name="libraryUids">the UIDs of the libraries</param>
    internal async Task DeleteFromLibraries(Guid[] libraryUids)
    {
        if (DbHelper.UseMemoryCache)
        {
            var allFiles = await base.GetDataList();
            var libFiles = allFiles.Where(x => x.Library?.Uid != null && libraryUids.Contains(x.Library.Uid)).Select(x => x.Uid).ToArray();
            await DeleteAll(libFiles);
        }
        else
        {
            await DbHelper.DeleteLibraryFilesFromLibraries(libraryUids);
        }
    }

    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    [HttpPost("search")]
    public async Task<IEnumerable<LibraryFile>> Search([FromBody] LibraryFileSearchModel filter)
    {
        if (DbHelper.UseMemoryCache == false)
            return await DbHelper.SearchLibraryFiles(filter);

        var results = await this.GetDataList();
        results = results.Where(x =>
            x.DateCreated >= filter.FromDate && x.DateCreated <= filter.ToDate && (string.IsNullOrEmpty(filter.Path) ||
                x.Name.ToLowerInvariant().Contains(filter.Path.ToLowerInvariant()))).Take(500);
        return results;

    }
}
