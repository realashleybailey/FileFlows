namespace FileFlows.Server.Workers
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Workers;
    using FileFlows.Shared.Helpers;

    public class PluginUpdaterWorker : Worker
    {
        public PluginUpdaterWorker() : base(ScheduleType.Hourly, 1)
        {
            Trigger();
        }

        protected override void Execute()
        {
            var settings = new SettingsController().Get().Result;
            if (settings?.AutoUpdatePlugins != true)
                return;

            Logger.Instance?.ILog("Plugin Updater started");
            var controller = new PluginController();
            var plugins = controller.GetDataList().Result;
            var latestPackages = controller.GetPluginPackages().Result;

            foreach(var plugin in plugins)
            {
                try
                {
                    var package = latestPackages?.Where(x => x?.Package == plugin?.PackageName)?.FirstOrDefault();
                    if (package == null)
                        continue; // no plugin, so no update

                    if (Version.Parse(package.Version) <= Version.Parse(plugin.Version))
                    {
                        // no new version, cannot update
                        continue;
                    }

                    string url = PluginController.PLUGIN_BASE_URL + "/download/" + package.Package;
                    if (url.EndsWith(".ffplugin") == false)
                        url += ".ffplugin";
                    var dlResult = HttpHelper.Get<byte[]>(url).Result;
                    if (dlResult.Success == false)
                    {
                        Logger.Instance.WLog($"Failed to download package '{plugin.PackageName}' update: " + dlResult.Body);
                        continue;
                    }
                    Helpers.PluginScanner.UpdatePlugin(package.Package, dlResult.Data);
                }
                catch(Exception ex)
                {
                    Logger.Instance.WLog($"Failed to update plugin '{plugin.PackageName}': " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
            Logger.Instance?.ILog("Plugin Updater finished");
        }
    }
}
