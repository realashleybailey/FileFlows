namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface ILibraryFileService
    {
        Task<LibraryFile> GetNext(string nodeName, Guid nodeUid, Guid workerUid);

        Task<LibraryFile> Get(Guid uid);

        Task Delete(Guid uid);

        Task<LibraryFile> Update(LibraryFile libraryFile);

        Task<bool> SaveFullLog(Guid uid, string log);
        Task<bool> ExistsOnServer(Guid uid);
    }

    public class LibraryFileService : Service, ILibraryFileService
    {

        public static Func<ILibraryFileService> Loader { get; set; }

        public static ILibraryFileService Load()
        {
            if (Loader == null)
                return new LibraryFileService();
            return Loader.Invoke();
        }

        public async Task Delete(Guid uid)
        {
            try
            {
                var result = await HttpHelper.Delete($"{ServiceBaseUrl}/api/library-file", new ReferenceModel { Uids = new[] { uid } });                
            }
            catch (Exception)
            {
                return;
            }
        }

        public async Task<bool> ExistsOnServer(Guid uid)
        {
            try
            {
                var result = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/api/library-file/exists-on-server/{uid}");
                if (result.Success == false)
                    return false;
                return result.Data;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LibraryFile> Get(Guid uid)
        {
            try
            {
                var result = await HttpHelper.Get<LibraryFile>($"{ServiceBaseUrl}/api/library-file/{uid}");
                if (result.Success == false)
                    return null;
                return result.Data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LibraryFile> GetNext(string nodeName, Guid nodeUid, Guid workerUid)
        {
            // can throw exception if nothing to process
            try
            {
                var result = await HttpHelper.Post<LibraryFile>($"{ServiceBaseUrl}/api/library-file/next-file", new NextLibraryFileArgs
                {
                    NodeName = nodeName,
                    NodeUid = nodeUid,
                    WorkerUid = workerUid,
                    NodeVersion = Globals.Version
                });
                if (result.Success == false)
                    return null; 
                return result.Data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> SaveFullLog(Guid uid, string log)
        {
            try
            {
                var result = await HttpHelper.Put<bool>($"{ServiceBaseUrl}/api/library-file/{uid}/full-log", log);
                if (result.Success)
                    return result.Data;
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LibraryFile> Update(LibraryFile libraryFile)
        {
            var result = await HttpHelper.Put<LibraryFile>($"{ServiceBaseUrl}/api/library-file", libraryFile);
            if (result.Success == false)
                throw new Exception("Failed to update library file: " + result.Body);
            return result.Data;
        }
    }
}
