namespace FileFlows.Server.Services;

using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryService : ILibraryService
{
    /// <summary>
    /// Gets a library by its UID
    /// </summary>
    /// <param name="uid">The UID of the library</param>
    /// <returns>An instance of the library if found</returns>
    public Task<Library> Get(Guid uid) => new LibraryController().Get(uid);
}