namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface ILibraryFileService
    {
        Task<LibraryFile> GetNext(Guid nodeUid, Guid workerUid);

        Task Delete(Guid uid);

        Task<LibraryFile> Update(LibraryFile libraryFile);
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
                var result = await HttpHelper.Delete($"{ServiceBaseUrl}/library-file", new ReferenceModel { Uids = new[] { uid } });                
            }
            catch (Exception)
            {
                return;
            }
        }

        public async Task<LibraryFile> GetNext(Guid nodeUid, Guid workerUid)
        {
            // can throw exception if nothing to process
            try
            {
                var result = await HttpHelper.Get<LibraryFile>($"{ServiceBaseUrl}/library-file/next-file?nodeUid={Uri.EscapeDataString(nodeUid.ToString())}&workerUid={Uri.EscapeDataString(workerUid.ToString())}");
                if (result.Success == false)
                    return null; 
                return result.Data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LibraryFile> Update(LibraryFile libraryFile)
        {
            var result = await HttpHelper.Put<LibraryFile>($"{ServiceBaseUrl}/library-file", libraryFile);
            if (result.Success == false)
                throw new Exception("Failed to update library file: " + result.Body);
            return result.Data;
        }
    }
}
