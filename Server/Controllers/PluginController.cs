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
        public async Task<IEnumerable<PluginInfoModel>> GetAll(bool includeElements = true)
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
                    Authors = plugin.Authors,
                    Url =  plugin.Url,
                    PackageName = plugin.PackageName,
                    Description = plugin.Description,   
                    Elements = includeElements ? plugin.Elements : null
                };
                var package = packages.FirstOrDefault(x => x.Name.ToLower().Replace(" ", "") == x.Name.ToLower().Replace(" ", ""));
                pim.LatestVersion = package?.Version ?? "";
                pims.Add(pim);
            }
            return pims;
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
        public async Task<IEnumerable<PluginPackageInfo>> GetPluginPackages([FromQuery] bool missing = false)
        {
            // should expose user configurable repositories
            var plugins = await HttpHelper.Get<IEnumerable<PluginPackageInfo>>(PLUGIN_BASE_URL + "?rand=" + System.DateTime.Now.ToFileTime());
            if (plugins.Success == false || plugins.Data == null)
                return new PluginPackageInfo[] { };

            if (missing)
            {
                // remove plugins already installed
                var installed = (await GetDataList()).Select(x => x.PackageName).ToList();
                return plugins.Data.Where(x => installed.Contains(x.Package) == false);
            }

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

            var items = await GetDataList();
            var deleting = items.Where(x => model.Uids.Contains(x.Uid));
            await DeleteAll(model);
            foreach(var item in deleting)
            {
                PluginScanner.Delete(item.PackageName);
            }

        }

        [HttpPost("download")]
        public async Task Download([FromBody] DownloadModel model)
        {
            if (model == null || model.Packages?.Any() != true)
                return; // nothing to delete

            foreach(var package in model.Packages)
            {
                try
                {
                    string dlPackage = package;
                    if (dlPackage.EndsWith(".ffplugin") == false)
                        dlPackage += ".ffplugin";
                    var dlResult = await HttpHelper.Get<byte[]>(PLUGIN_BASE_URL + "/download/" + dlPackage);
                    if (dlResult.Success)
                        PluginScanner.UpdatePlugin(package, dlResult.Data);
                }
                catch (Exception ex)
                { 
                    Logger.Instance?.ELog($"Failed downloading plugin package: '{package}' => {ex.Message}");
                }
            }
        }

        [HttpPost("download-package")]
        public async Task<byte[]> DownloadPackage([FromBody] PluginInfo model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            var actual = await Get(model.Uid);
            if (actual == null)
                throw new Exception("UID not found");

            if(actual.Version != model.Version)
                throw new Exception("Version mismatch");

            string dir = PluginScanner.GetPluginDirectory();
            string file = Path.Combine(dir, actual.PackageName);
            if (file.EndsWith(".ffplugin") == false)
                file += ".ffplugin";

            if (System.IO.File.Exists(file) == false)
                throw new Exception("File not found");

            byte[] data = System.IO.File.ReadAllBytes(file);
            return data;
        }

        public class DownloadModel
        {
            public List<string> Packages { get; set; }
        }
    }
}