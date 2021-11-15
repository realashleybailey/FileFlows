namespace FileFlow.Server.Controllers
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Server;
    using FileFlow.Shared.Models;
    using FileFlow.Server.Helpers;

    [Route("/api/settings")]
    public class SettingsController : Controller
    {

        [HttpGet]
        public Settings Get()
        {
            return DbHelper.Single<Settings>() ?? new Settings();
        }

        [HttpPut]
        public Settings Save([FromBody] Settings model)
        {
            if (model == null)
                return Get();
            var settings = Get() ?? model;
            model.Uid = settings.Uid;
            model.DateCreated = settings.DateCreated;
            return DbHelper.Update(model);
        }
    }

}