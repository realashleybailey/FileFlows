namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class LibraryFileService : ILibraryFileService
    {
        public Task Delete(Guid uid) => new LibraryFileController().Delete(new ReferenceModel {  Uids = new [] { uid } });  

        public Task<LibraryFile> GetNext(Guid nodeUid, Guid workerUid) => new LibraryFileController().GetNext(nodeUid, workerUid);

        public Task<LibraryFile> Update(LibraryFile libraryFile) => new LibraryFileController().Update(libraryFile);
    }
}
