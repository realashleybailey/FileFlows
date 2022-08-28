using Esprima.Ast;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public class LibraryFileService : ILibraryFileService
{
    private static Dictionary<Guid, LibraryFile> CachedData;

    private async Task<NPoco.Database> GetDbWithMappings()
    {
        var db = await DbHelper.GetDbManager().GetDb();
        db.Db.Mappers.Add(new CustomDbMapper());
        return db.Db;
    }

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public async Task<LibraryFile> Get(Guid uid)
    {
        lock (CachedData)
        {
            if (CachedData?.ContainsKey(uid) == true)
                return CachedData[uid];
        }

        using var db = await GetDbWithMappings();
        return await db.SingleAsync<LibraryFile>("select * from LibraryFile where Uid = @0", uid);
    }

    

    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, Guid workerUid) 
        => new LibraryFileController().GetNext(new NextLibraryFileArgs
            {
                 NodeName   = nodeName,
                 NodeUid = nodeUid,
                 WorkerUid = workerUid,
                 NodeVersion = Globals.Version.ToString()
            });

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    public Task<bool> SaveFullLog(Guid uid, string log) => new LibraryFileController().SaveFullLog(uid, log);


    /// <summary>
    /// Adds a library file
    /// </summary>
    /// <param name="file">the library file to add</param>
    /// <returns>the added library file</returns>
    public async Task<LibraryFile> Add(LibraryFile file)
    {
        if(file.Uid == Guid.Empty)
            file.Uid = Guid.NewGuid();
        using (var db = await GetDbWithMappings())
        {
            await db.InsertAsync(file);
        }

        return await Get(file.Uid);
    }
    /// <summary>
    /// Adds a many library file
    /// </summary>
    /// <param name="files">the library files to add</param>
    /// <returns>an awaited task</returns>
    public async Task AddMany(params LibraryFile[] files)
    {
        if (files?.Any() != true)
            return;

        foreach (var file in files)
        {
            if (file == null)
                continue;
            if(file.Uid == Guid.Empty)
                file.Uid = Guid.NewGuid();
        }

        var db = await GetDbWithMappings();
        await db.InsertBulkAsync(files.Where(x => x != null));
    }
    
    /// <summary>
    /// Updates a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    public async Task<LibraryFile> Update(LibraryFile file)
    {
        // keep this in the controller
        // var existing = await Get(file.Uid);
        // if (existing == null)
        //     throw new Exception("Not found");
        //
        // if (existing.Status == FileStatus.Processed && file.Status == FileStatus.Processing)
        // {
        //     // already finished and reported finished in the Worker controller.  So ignore this out of date update
        //     return existing;
        // }
        //
        // if (existing.Status != file.Status)
        // {
        //     Logger.Instance?.ILog($"Setting library file status to: {file.Status} - {file.Name}");
        //     existing.Status = file.Status;
        // }
        //
        // existing.Node = file.Node;
        // if(existing.FinalSize == 0 || file.FinalSize > 0)
        //     existing.FinalSize = file.FinalSize;
        // if(file.OriginalSize > 0)
        //     existing.OriginalSize = file.OriginalSize;
        // if(string.IsNullOrEmpty(file.OutputPath))
        //     existing.OutputPath = file.OutputPath;
        // existing.Flow = file.Flow;
        // if(file.Library != null && file.Library.Uid == existing.Library.Uid)
        //     existing.Library = file.Library; // name may have changed and is being updated
        //
        // existing.DateCreated = file.DateCreated; // this can be changed if library file is unheld
        // existing.ProcessingEnded = file.ProcessingEnded;
        // existing.ProcessingStarted = file.ProcessingStarted;
        // existing.WorkerUid = file.WorkerUid;
        // existing.ExecutedNodes = file.ExecutedNodes ?? new List<ExecutedNode>();
        // if (file.OriginalMetadata?.Any() == true)
        //     existing.OriginalMetadata = file.OriginalMetadata;
        // if (file.FinalMetadata?.Any() == true)
        //     existing.FinalMetadata = file.FinalMetadata;

        using (var db = await GetDbWithMappings())
        {
            await db.UpdateAsync(file);
        }

        lock (CachedData)
        {
            if (CachedData?.ContainsKey(file.Uid) == true)
                CachedData[file.Uid] = file;
        }
        
        return file;
    }

    /// <summary>
    /// Deletes library files
    /// </summary>
    /// <param name="uids">a list of UIDs to delete</param>
    /// <returns>an awaited task</returns>
    public async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'${x}'"));
        using (var db = await GetDbWithMappings())
        {
            await db.ExecuteAsync($"delete from LibraryFile where Uid in ({inStr})", null);
        }

        lock (CachedData)
        {
            foreach (var uid in uids)
            {
                if (CachedData?.ContainsKey(uid) == true)
                    CachedData.Remove(uid);
            }
        }
    }


    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public Task<bool> ExistsOnServer(Guid uid) => new LibraryFileController().ExistsOnServer(uid);

    /// <summary>
    /// Get all the library file UIDs in the database
    /// </summary>
    /// <returns>all the library file UIDs in the database</returns>
    public async Task<IEnumerable<Guid>> GetUids()
    {
        var db = await GetDbWithMappings();
        return await db.FetchAsync<Guid>("select Uid from LibraryFile");
    }

    /// <summary>
    /// Gets all matching library files
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="skip">the amount to skip</param>
    /// <param name="rows">the number to fetch</param>
    /// <returns>a list of matching library files</returns>
    public async Task<IEnumerable<LibraryFile>> GetAll(FileStatus? status, int skip = 0, int rows = 0)
    {
        try
        {
            if ((int)status > 0)
            {
                // the status in the db is correct and not a computed status
                string sqlStatus =
                    SqlHelper.Skip($"select * from LibraryFile where Status = {(int)status} order by DateModified desc",
                        skip, rows);
                var dbStatus = await GetDbWithMappings();
                var result = await dbStatus.FetchAsync<LibraryFile>(sqlStatus);
                return result;
            }
            
            var libraries = await new LibraryController().GetAll();
            var disabled = string.Join(", ",
                libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
            int quarter = TimeHelper.GetCurrentQuarter();
            var outOfSchedule = string.Join(", ",
                libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));
            List<string> libWheres = new();
            foreach(var lib in libraries)
            {
                if (lib.HoldMinutes < 1)
                    continue;
                libWheres.Add(
                    $"(LibraryUid = '{lib.Uid}' and DateCreated > DATE_ADD(NOW(), INTERVAL -{lib.HoldMinutes} minute))");
            }

            string sql = $"select * from LibraryFile where Status = {(int)FileStatus.Unprocessed}";
            
            // add disabled condition
            if(string.IsNullOrEmpty(disabled) == false)
                sql += $" and LibraryUid {(status == FileStatus.Disabled ? "" : "not")} in ({disabled})";
            
            var db = await GetDbWithMappings();
            if (status == FileStatus.Disabled)
            {
                if (string.IsNullOrEmpty(disabled))
                    return new List<LibraryFile>(); // no disabled libraries
                return await db.FetchAsync<LibraryFile>(SqlHelper.Skip(sql + " order by DateModified desc", skip, rows));
            }
            
            // add out of schedule condition
            if(string.IsNullOrEmpty(outOfSchedule) == false)
                sql += $" and LibraryUid {(status == FileStatus.OutOfSchedule ? "" : "not")} in ({outOfSchedule})";
            
            if (status == FileStatus.OutOfSchedule)
            {
                if (string.IsNullOrEmpty(outOfSchedule))
                    return new List<LibraryFile>(); // no out of schedule libraries
                
                return await db.FetchAsync<LibraryFile>(SqlHelper.Skip(sql + " order by DateModified desc", skip, rows));
            }
            
            // add on hold condition
            if (libWheres.Any())
                sql += $" and (" + string.Join(" or ", libWheres) + ") " +
                       (status != FileStatus.OnHold ? " = false" : "");

            if (status == FileStatus.OnHold)
            { 
                if(libWheres.Any() == false)
                    return new List<LibraryFile>(); // no libraries with hold
                return await db.FetchAsync<LibraryFile>(SqlHelper.Skip(sql + " order by DateModified desc", skip, rows));
            }

            sql = SqlHelper.Skip(sql + " order by case when ProcessingOrder > 0 then ProcessingOrder else 1000000 end, DateModified desc", skip, rows);
            return  await db.FetchAsync<LibraryFile>(sql);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetAll Files: " + ex.Message);
            return new LibraryFile[] { };
        }
    }

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="uids">a list of UIDs to reprocess</param>
    public async Task Reprocess(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'${x}'"));
        var db = await GetDbWithMappings();
        await db.ExecuteAsync($"update LibraryFile set Status = 0 where Uid in ({inStr})");
    }
    
    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    internal async Task ResetProcessingStatus(Guid? nodeUid = null)
    {
        var db = await GetDbWithMappings();
        if(nodeUid != null)
            await db.ExecuteAsync($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing} and NodeUid = '{nodeUid}", null);
        else
            await db.ExecuteAsync($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing}", null);
    }

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <returns>the library file if it is known</returns>
    public async Task<LibraryFile> GetFileIfKnown(string path)
    {
        var db = await GetDbWithMappings();
        return await db.SingleAsync<LibraryFile>("select * from LibraryFile where Name = @0 or OutputPath @0", path);
    }
    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public async Task<LibraryFile> GetFileByFingerprint(string fingerprint)
    {
        var db = await GetDbWithMappings();
        return await db.SingleAsync<LibraryFile>("select * from LibraryFile where fingerprint = @0", fingerprint);
    }

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="includeOutput">if output names should be included</param>
    /// <returns>a list of all filenames</returns>
    public async Task<Dictionary<string, DateTime>> GetKnownLibraryFilesWithCreationTimes(bool includeOutput = false)
    {
        var db = await GetDbWithMappings();
        var list = await db.FetchAsync<(string, DateTime)>("select Name, CreationTime from LibraryFile");
        if (includeOutput == false)
            return list.DistinctBy(x => x.Item1.ToLowerInvariant()).ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2);
        var outputs = await db.FetchAsync<(string, DateTime)>("select OutputPath, CreationTime from LibraryFile");
        return list.Union(outputs).Where(x => string.IsNullOrEmpty(x.Item1) == false).DistinctBy(x => x.Item1.ToLowerInvariant())
            .ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2);
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public async Task<IEnumerable<LibraryStatus>> GetStatus()
    {
        // if (DbHelper.UseMemoryCache)
        // {
        //     var libraryFiles = await GetDataList();
        //     var libraries = await new LibraryController().GetData();
        //     return GetStatusData(libraryFiles, libraries);
        // }
        // else
        {
            var result = await DbHelper.GetLibraryFileOverview();
            return result;
        }
    }

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public async Task MoveToTop(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string strUids = string.Join(", ", uids.Select(x => "'" + x + "'"));
        // get existing order first so we can shift those if these uids change the order
        // only get status == 0
        var db = await GetDbWithMappings();
        List<Guid> indexed = uids.ToList();
        var sorted = await db.FetchAsync<LibraryFile>($"select * from LibraryFile where Status = 0 and ( ProcessingOrder > 0 or Uid IN ({strUids}))");
        sorted = sorted.OrderBy(x =>
        {
            int index = indexed.IndexOf(x.Uid);
            if (index < 0)
                return 10000 + x.Order;
            return index;
        }).ToList();

        var commands = new List<string>();
        for(int i=0;i<sorted.Count;i++)
        {
            var file = sorted[i];
            file.Order = i + 1;
            commands.Add($"update LibraryFile set ProcessingOrder = {file.Order} where Uid = '{file.Uid}';");
        }

        await db.ExecuteAsync(string.Join("\n", commands));
    }
}