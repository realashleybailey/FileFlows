namespace FileFlows.Server.Services
{
    using FileFlows.ServerShared.Services;

    internal class InitServices
    {
        public static void Init()
        {
            //Node.FlowExecution.FlowRunnerCommunicator.SignalrUrl = Controllers.NodeController.SignalrUrl;

            ServerShared.Services.SettingsService.Loader = () => new SettingsService();
            ServerShared.Services.NodeService.Loader = () => new NodeService();
            ServerShared.Services.PluginService.Loader = () => new PluginService();
            ServerShared.Services.FlowService.Loader = () => new FlowService();
            ServerShared.Services.FlowRunnerService.Loader = () => new FlowRunnerService();
            ServerShared.Services.LibraryService.Loader = () => new LibraryService();
            ServerShared.Services.LibraryFileService.Loader = () => new LibraryFileService();
        }
    }
}
