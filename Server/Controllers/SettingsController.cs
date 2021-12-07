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
        public int IsConfigured()
        {
            var libs = new LibraryController().GetData().Result?.Any() == true;
            var flows = new FlowController().GetData().Result?.Any() == true;
            if (libs && flows)
                return 2;
            if (flows)
                return 1;
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
                    Instance = await DbManager.Single<Settings>();
                return Instance;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
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
            return await DbManager.Update(model);
        }
    }

}