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
            if (Globals.Demo)
                return new Settings { LoggingPath = "/app/Logs", TempPath = "/temp", WorkerFlowExecutor = true, WorkerScanner = true, DisableTelemetry = false };

            return DbHelper.Single<Settings>() ?? new Settings();
        }

        [HttpPut]
        public Settings Save([FromBody] Settings model)
        {
            if (Globals.Demo)
                return model;

            if (model == null)
                return Get();
            var settings = Get() ?? model;
            model.Uid = settings.Uid;
            model.DateCreated = settings.DateCreated;
            return DbHelper.Update(model);
        }

        [HttpGet("telemetry")]
        public bool Telemetry()
        {
            if (Globals.Demo)
                return false;

            var settings = Get();
            return settings?.DisableTelemetry != true;
        }
    }

}