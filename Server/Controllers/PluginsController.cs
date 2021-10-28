namespace ViWatcher.Server.Controllers
{
    using System.ComponentModel;
    using System.Dynamic;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using ViWatcher.Plugins.Attributes;
    using ViWatcher.Server.Helpers;
    using ViWatcher.Shared.Models;

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

            pi.Fields = new List<ElementField>();
            foreach (var prop in plugin.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = prop.GetCustomAttributes(typeof(FormInputAttribute), false).FirstOrDefault() as FormInputAttribute;
                if (attribute != null)
                {
                    pi.Fields.Add(new ElementField
                    {
                        Name = prop.Name,
                        Order = attribute.Order,
                        InputType = attribute.InputType,
                        Type = prop.PropertyType.FullName
                    });
                    if (dict.ContainsKey(prop.Name) == false)
                    {
                        var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                        dict.Add(prop.Name, dValue != null ? dValue.Value : prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null);
                    }
                }
            }
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
    }
}