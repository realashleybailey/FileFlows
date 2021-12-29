namespace FileFlows.Server.Controllers
{
    using System.ComponentModel;
    using System.Dynamic;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Plugin.Attributes;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;
    using FileFlows.Shared.Helpers;
    using FileFlows.ServerShared.Helpers;

    [Route("/api/plugin")]
    public class PluginController : ControllerStore<PluginInfo>
    {
        const string PLUGIN_BASE_URL = "https://fileflows.com/api/plugin";
        [HttpGet]
        public async Task<IEnumerable<PluginInfoModel>> GetAll(bool includeElements = false)
        {
            var plugins = (await GetDataList()).Where(x => x.Deleted == false);
            List<PluginInfoModel> pims = new List<PluginInfoModel>();
            var packages = await GetPluginPackages();
            foreach (var plugin in plugins)
            {
                var pim = new PluginInfoModel
                {
                    Uid = plugin.Uid,
                    Name = plugin.Name,
                    DateCreated = plugin.DateCreated,
                    DateModified = plugin.DateModified,
                    Enabled = plugin.Enabled,
                    Version = plugin.Version,
                    Deleted = plugin.Deleted,
                    HasSettings = plugin.HasSettings,
                    Settings = plugin.Settings,
                    Fields = plugin.Fields,
                    Elements = includeElements ? plugin.Elements : null
                };
                var package = packages.FirstOrDefault(x => x.Name.ToLower().Replace(" ", "") == x.Name.ToLower().Replace(" ", ""));
                pim.LatestVersion = package?.Version ?? "";
                pims.Add(pim);
            }
            return pims;
        }

        [HttpPut("state/{uid}")]
        public async Task<PluginInfo> SetState([FromRoute] Guid uid, [FromQuery] bool enable)
        {
            var plugin = await GetByUid(uid);;
            if (plugin == null)
                throw new Exception("Plugin not found.");

            if (plugin.Name == "Basic Nodes" && enable == false)
                return plugin; // dont let them disable the basic nodes
            if (enable == plugin.Enabled)
                return plugin;
                        
            plugin.Enabled = enable;
            return await Update(plugin);
        }

        [HttpGet("{uid}")]
        public async Task<PluginInfo> Get([FromRoute] Guid uid)
        {
            var pi = await GetByUid(uid);
            return pi ?? new();
        }

        [HttpPost("{uid}/settings")]
        public async Task<PluginInfo> SaveSettings([FromRoute] Guid uid, [FromBody] ExpandoObject settings)
        {
            var pi = await GetByUid(uid);
            if (pi == null)
                return new PluginInfo();
            pi.Settings = settings;
            return await Update(pi);
        }

        private string GetPluginsDir() => Path.Combine(Program.GetAppDirectory(), "Plugins");

        [HttpGet("language/{langCode}.json")]
        public IActionResult LanguageFile([FromQuery] string langCode = "en")
        {
            return File("i18n/plugins.en.json", "text/json");
        }

        [HttpGet("plugin-packages")]
        public async Task<IEnumerable<PluginPackageInfo>> GetPluginPackages()
        {
            // should expose user configurable repositories
            var plugins = await HttpHelper.Get<IEnumerable<PluginPackageInfo>>(PLUGIN_BASE_URL + "?rand=" + System.DateTime.Now.ToFileTime());
            if (plugins.Success == false || plugins.Data == null)
                return new PluginPackageInfo[] { };
            return plugins.Data;
        }


        [HttpPost("update/{uid}")]
        public async Task<bool> Update([FromRoute] Guid uid)
        {
            var plugin = await Get(uid);
            if (plugin == null)
                return false;
            var plugins = await GetPluginPackages();
            var ppi = plugins.FirstOrDefault(x => x.Name.Replace(" ", "").ToLower() == plugin.Name.Replace(" ", "").ToLower());
            if (ppi == null)
                return false;

            if (Version.Parse(ppi.Version) <= Version.Parse(plugin.Version))
            {
                // no new version, cannot update
                return false;
            }

            var dlResult = await HttpHelper.Get<byte[]>(PLUGIN_BASE_URL + "/download/" + ppi.Package);
            if (dlResult.Success == false)
                return false;

            // save the zip and unzip it
            return PluginScanner.UpdatePlugin(ppi.Package, dlResult.Data);
        }

        [HttpDelete]
        public async Task Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            await DeleteAll(model);
        }
    }
}