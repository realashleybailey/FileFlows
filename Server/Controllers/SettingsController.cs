namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

    [Route("/api/settings")]
    public class SettingsController : Controller
    {
        private static Settings Instance = new ();
        private static Mutex _mutex = new Mutex();

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