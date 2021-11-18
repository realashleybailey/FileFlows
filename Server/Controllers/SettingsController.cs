namespace FileFlows.Server.Controllers
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;

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