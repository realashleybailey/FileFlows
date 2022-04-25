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

        public Task<LibraryFile> Get(Guid uid) => new LibraryFileController().Get(uid);

        public Task<LibraryFile> GetNext(string nodeName, Guid nodeUid, Guid workerUid) => new LibraryFileController().GetNext(nodeName, nodeUid, workerUid);

        public Task<bool> SaveFullLog(Guid uid, string log) => new LibraryFileController().SaveFullLog(uid, log);

        public Task<LibraryFile> Update(LibraryFile libraryFile) => new LibraryFileController().Update(libraryFile);

        public Task<bool> ExistsOnServer(Guid uid) => new LibraryFileController().ExistsOnServer(uid);

    }
}
