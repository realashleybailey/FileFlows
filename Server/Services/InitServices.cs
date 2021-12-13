namespace FileFlows.Server.Services
{
    using FileFlows.ServerShared.Services;

    internal class InitServices
    {
        public static void Init()
        {
            ServerShared.Services.SettingsService.Loader = () => new SettingsService();
            ServerShared.Services.PluginService.Loader = () => new PluginService();
            ServerShared.Services.FlowService.Loader = () => new FlowService();
            ServerShared.Services.LibraryService.Loader = () => new LibraryService();
            ServerShared.Services.LibraryFileService.Loader = () => new LibraryFileService();
        }
    }
}
