using FileFlows.Server.Database.Managers;
using FileFlows.Shared;

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
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

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
            if (settings.DisableTelemetry != false)
                return string.Empty; 
            try
            {
                var result = Workers.ServerUpdater.GetLatestOnlineVersion();
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
            await semaphore.WaitAsync();
            try
            {
                if (Instance == null)
                {
                    Instance = await DbHelper.Single<Settings>();

                }
                Instance.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                Instance.IsDocker = Program.Docker;
                return Instance;
            }
            finally
            {
                semaphore.Release();
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
            
            return await DbHelper.Update(model);
        }

        /// <summary>
        /// Tests a database connection
        /// </summary>
        /// <param name="model">The database connection info</param>
        /// <returns>OK if successful, otherwise a failure message</returns>
        [HttpPost("test-db-connection")]
        public string TestDbConnection([FromBody] DbConnectionInfo model)
        {
            if (model == null)
                throw new ArgumentException(nameof(model));

            if (model.Type == DatabaseType.SqlServer)
                return new SqlServerDbManager(string.Empty).Test(model.Server, model.Name, model.User, model.Password)
                    ?.EmptyAsNull() ?? "OK";
            if (model.Type == DatabaseType.MySql)
                return new MySqlDbManager(string.Empty).Test(model.Server, model.Name, model.User, model.Password)
                    ?.EmptyAsNull() ?? "OK";
            
            return "Unsupported database type";
        }
    }

    /// <summary>
    /// Database connection details
    /// </summary>
    public class DbConnectionInfo
    {
        /// <summary>
        /// Gets or sets the server address
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// Gets or sets the database name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the connecting user
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Gets or sets the password used
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the database type
        /// </summary>
        public DatabaseType Type { get; set; }
    }
}