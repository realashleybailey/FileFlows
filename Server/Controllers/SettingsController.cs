namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;
    using System.Runtime.InteropServices;

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
        /// <param name="timeZoneId">The string timezone ID of the system</param>
        /// <returns>return 2 if everything is configured, 1 if partially configured, 0 if not configured</returns>
        [HttpGet("is-configured")]
        public async Task<int> IsConfigured([FromQuery(Name = "tz")] string timeZoneId)
        {
            // this updates the TZ with the TZ from the client if not set
            var settings = await Get();
            if (string.IsNullOrEmpty(settings.TimeZone))
            {
                settings.TimeZone = timeZoneId;
                await Save(settings);
            }

            var libs = new LibraryController().GetData().Result?.Any() == true;
            var flows = new FlowController().GetData().Result?.Any() == true;
            if (libs && flows)
                return 2;
            if (flows)
                return 1;
            return 0;
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

                    InitTimeZone(Instance);
                }
                Instance.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                return Instance;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void InitTimeZone(Settings settings)
        {
            TimeHelper.UserTimeZone = settings.TimeZone;
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
            Instance = model;
            InitTimeZone(Instance);
            
            return await DbManager.Update(model);
        }
    }

}