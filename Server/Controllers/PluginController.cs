namespace FileFlow.Server.Controllers
{
    using System.ComponentModel;
    using System.Dynamic;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Plugin.Attributes;
    using FileFlow.Server.Helpers;
    using FileFlow.Shared.Models;

    [Route("/api/plugin")]
    public class PluginController : Controller
    {
        [HttpGet]
        public IEnumerable<PluginInfo> GetAll()
        {
            return DbHelper.Select<PluginInfo>().Where(x => x.Deleted == false);
        }

        [HttpPut("state/{uid}")]
        public PluginInfo SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            var plugin = DbHelper.Single<PluginInfo>(uid);
            if (plugin == null)
                throw new Exception("Plugin not found.");

            if (plugin.Name == "Basic Nodes" && enable == false)
                return plugin; // dont let them disable the basic nodes
            if (enable != null)
            {
                plugin.Enabled = enable.Value;
                DbHelper.Update(plugin);
            }
            return plugin;
        }

        [HttpGet("{uid}")]
        public PluginInfo Get([FromRoute] Guid uid)
        {
            var pi = DbHelper.Single<PluginInfo>(uid);
            if (pi == null)
                return new PluginInfo();

            var plugin = PluginHelper.GetPlugin(pi.Assembly);
            if (plugin == null)
                return pi;
            // get the fields for the plugin.

            pi.Settings ??= new System.Dynamic.ExpandoObject();
            var dict = (IDictionary<string, object>)pi.Settings;

            pi.Fields = FormHelper.GetFields(plugin.GetType(), dict);
            return pi;
        }

        [HttpPost("{uid}/settings")]
        public PluginInfo SaveSettings([FromRoute] Guid uid, [FromBody] ExpandoObject settings)
        {
            var pi = DbHelper.Single<PluginInfo>(uid);
            if (pi == null)
                return new PluginInfo();
            pi.Settings = settings;
            DbHelper.Update(pi);
            return pi;
        }

        [HttpGet("language/{langCode}.json")]
        public string LanguageFile([FromQuery] string langCode = "en")
        {
            var json = "{}";
            foreach (var jf in Directory.GetFiles("plugins", "*.json"))
            {
                if (jf.Contains(".deps."))
                    continue;
                try
                {
                    string updated = JsonHelper.Merge(json, System.IO.File.ReadAllText(jf));
                    json = updated;
                }
                catch (Exception) { }
            }
            return json;
        }
    }
}