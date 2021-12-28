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
                    Assembly = plugin.Assembly,
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
        public string LanguageFile([FromQuery] string langCode = "en")
        {
            var json = "{}";
            try
            {
                foreach (var dir in new DirectoryInfo(GetPluginsDir()).GetDirectories())
                {
                    foreach (var jf in dir.GetFiles("*.json"))
                    {
                        if (jf.Name.Contains(".deps."))
                            continue;
                        try
                        {
                            string updated = JsonHelper.Merge(json, System.IO.File.ReadAllText(jf.FullName));
                            json = updated;
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.ELog("Error loading plugin json[0]:" + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error loading plugin json[1]:" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return json;
        }

        [HttpGet("plugin-packages")]
        public async Task<IEnumerable<PluginPackageInfo>> GetPluginPackages()
        {
            // should expose user configurable repositories
            var plugins = await HttpHelper.Get<IEnumerable<PluginPackageInfo>>("https://github.com/revenz/FileFlowsPlugins/blob/master/plugins.json?raw=true&rand=" + System.DateTime.Now.ToFileTime());
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

            var zipResult = await HttpHelper.Get<byte[]>(ppi.Package);
            if (zipResult.Success == false)
                return false;

            // save the zip and unzip it
            string zipFile = System.IO.Path.Combine("Plugins", plugin.Name + ".zip");
            System.IO.File.WriteAllBytes(zipFile, zipResult.Data);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, $"Plugins/{(plugin.Name.Replace(" ", ""))}/{ppi.Version}", true);

            System.IO.File.Delete(zipFile);

            plugin.Version = ppi.Version;
            plugin.Fields = null;
            await Update(plugin);
            return true;
        }
    }
}