namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;
    using System.Runtime.InteropServices;
    using FileFlows.Shared.Helpers;

    /// <summary>
    /// Settings Controller
    /// </summary>
    [Route("/api/settings")]
    public class SettingsController : Controller
    {
        private static Settings Instance;
        private static Mutex _mutex = new Mutex();

        /// <summary>
        /// Whether or not the system is configured
        /// </summary>
        /// <returns>return 2 if everything is configured, 1 if partially configured, 0 if not configured</returns>
        [HttpGet("is-configured")]
        public async Task<int> IsConfigured()
        {
            // this updates the TZ with the TZ from the client if not set
            var settings = await Get();

            var libs = new LibraryController().GetData().Result?.Any() == true;
            var flows = new FlowController().GetData().Result?.Any() == true;
            if (libs && flows)
                return 2;
            if (flows)
                return 1;
            return 0;
        }

        /// <summary>
        /// Checks latest version from fileflows.com
        /// </summary>
        /// <returns>The latest version number if greater than current</returns>
        [HttpGet("check-update-available")]
        public async Task<string> CheckLatestVersion()
        {
            var settings = await new SettingsController().Get();
            if (settings.IsWindows == true && settings.AutoUpdate == true)
                return string.Empty; // we let the auto updater take care of this.
            try
            {
                var result = Workers.AutoUpdater.GetLatestOnlineVersion();
                if (result.updateAvailable == false)
                    return string.Empty;
                return result.onlineVersion.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed checking latest version: " + ex.Message + Environment.NewLine + ex.StackTrace);
                return String.Empty;
            }
        }

        /// <summary>
        /// Get the system settings
        /// </summary>
        /// <returns>The system settings</returns>
        [HttpGet]
        public async Task<Settings> Get()
        {
            if (Instance != null)
                return Instance;
            _mutex.WaitOne();
            try
            {
                if (Instance == null)
                {
                    Instance = await DbManager.Single<Settings>();

                }
                Instance.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                Instance.IsDocker = Program.Docker;
                return Instance;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Save the system settings
        /// </summary>
        /// <param name="model">the system settings to save</param>
        /// <returns>The saved system settings</returns>
        [HttpPut]
        public async Task<Settings> Save([FromBody] Settings model)
        {
            if (model == null)
                return await Get();
            var settings = await Get() ?? model;
            model.Uid = settings.Uid;
            model.DateCreated = settings.DateCreated;
            model.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            model.IsDocker = Program.Docker;
            Instance = model;
            
            return await DbManager.Update(model);
        }
    }

}