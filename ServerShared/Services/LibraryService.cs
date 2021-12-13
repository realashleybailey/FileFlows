namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface ILibraryService
    {
        Task<Library> Get(Guid uid);
    }

    public class LibraryService : Service, ILibraryService
    {

        public static Func<ILibraryService> Loader { get; set; }

        public static ILibraryService Load()
        {
            if (Loader == null)
                return new LibraryService();
            return Loader.Invoke();
        }

        public async Task<Library> Get(Guid uid)
        {
            try
            {
                var result = await HttpHelper.Get<Library>($"{ServiceBaseUrl}/library/" + uid.ToString());
                if (result.Success == false)
                    throw new Exception("Failed to locate library: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to get library: " + uid + " => " + ex.Message);
                return null;
            }
        }
    }
}
