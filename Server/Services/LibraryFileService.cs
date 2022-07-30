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
    /// <summary>
    /// Deletes a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>a completed task</returns>
    public Task Delete(Guid uid) => new LibraryFileController().Delete(new ReferenceModel<Guid> {  Uids = new [] { uid } });

    
    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public Task<LibraryFile> Get(Guid uid) => new LibraryFileController().Get(uid);

    

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
    /// Updates a library file
    /// </summary>
    /// <param name="libraryFile">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    public Task<LibraryFile> Update(LibraryFile libraryFile) => new LibraryFileController().Update(libraryFile);

    
    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public Task<bool> ExistsOnServer(Guid uid) => new LibraryFileController().ExistsOnServer(uid);

}