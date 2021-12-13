namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class LibraryService : ILibraryService
    {
        public Task<Library> Get(Guid uid) => new LibraryController().Get(uid);
    }
}
