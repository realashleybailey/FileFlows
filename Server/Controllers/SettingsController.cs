namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;
    using global::Server.Helpers;

    [Route("/api/settings")]
    public class SettingsController : Controller
    {

        [HttpGet]
        public Settings Get() => CacheStore.GetSettings();

        [HttpPut]
        public Settings Save([FromBody] Settings model)
        {
            if (model == null)
                return Get();
            var settings = Get() ?? model;
            model.Uid = settings.Uid;
            model.DateCreated = settings.DateCreated;
            var result = DbHelper.Update(model);
            CacheStore.SetSettings(result);
            return result;
        }
    }

}