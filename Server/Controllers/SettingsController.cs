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
            return DbHelper.Single<Settings>();
        }

        [HttpPut]
        public void Save([FromBody] Settings model)
        {
            if (model == null)
                return;
            model.Extensions = model.Extensions?.Where(x => x != null)?.Select(x =>
            {
                if (x.StartsWith("."))
                    return x.Substring(1).ToLower();
                return x.ToLower();
            })?.ToArray() ?? new string[] { };
            DbHelper.Update(model);
        }
    }

}