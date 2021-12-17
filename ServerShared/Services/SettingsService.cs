namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface ISettingsService
    {
        Task<Settings> Get();
    }

    public class SettingsService : Service, ISettingsService
    {

        public static Func<ISettingsService> Loader { get; set; }

        public static ISettingsService Load()
        {
            if (Loader == null)
                return new SettingsService();
            return Loader.Invoke();
        }

        public async Task<Settings> Get()
        {
            try
            {
                var result = await HttpHelper.Get<Settings>($"{ServiceBaseUrl}/api/settings");
                if (result.Success == false)
                    throw new Exception("Failed to get settings: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to get settings: " + ex.Message);
                return null;
            }
        }
    }
}
