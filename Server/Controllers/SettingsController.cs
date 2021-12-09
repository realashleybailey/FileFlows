namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

    [Route("/api/settings")]
    public class SettingsController : Controller
    {
        private static Settings Instance;
        private static Mutex _mutex = new Mutex();

        [HttpGet("is-configured")]
        public async Task<int> IsConfigured([FromQuery(Name = "tz")] string timeZoneId)
        {
            var libs = new LibraryController().GetData().Result?.Any() == true;
            var flows = new FlowController().GetData().Result?.Any() == true;
            if (libs && flows)
                return 2;
            if (flows)
                return 1;
            var settings = await Get();
            if (string.IsNullOrEmpty(settings.TimeZone))
            {
                settings.TimeZone = timeZoneId;
                await Save(settings);
            }
            return 0;
        }

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
                return Instance;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void InitTimeZone(Settings settings)
        {
            TimeHelper.UserTimeZone = TimeZoneInfo.GetSystemTimeZones().Where(x => x.Id == settings.TimeZone).FirstOrDefault();
        }

        [HttpPut]
        public async Task<Settings> Save([FromBody] Settings model)
        {
            if (model == null)
                return await Get();
            var settings = await Get() ?? model;
            model.Uid = settings.Uid;
            model.DateCreated = settings.DateCreated;
            Instance = model;
            InitTimeZone(Instance);
            
            return await DbManager.Update(model);
        }
    }

}