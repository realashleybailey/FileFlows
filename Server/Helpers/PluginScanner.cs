using FileFlows.Server.Controllers;
using FileFlows.Shared.Models;
using System.Text.Json;

namespace FileFlows.Server.Helpers
{
    public class PluginScanner
    {
        public static void Scan()
        {
            Logger.Instance.ILog("Scanning for plugins");
            var pluginDir = Path.Combine(Program.GetAppDirectory(), "Plugins");
            Logger.Instance.ILog("Plugin path:" + pluginDir);

            var controller = new PluginController();
            var dbPluginInfos = controller.GetAll().Result.Select(x => (PluginInfo)x).ToList();

            List<string> installed = new List<string>();
            var options = new JsonSerializerOptions
            {
                Converters = { new Shared.Json.ValidatorConverter() }
            };

            foreach (string ffplugin in Directory.GetFiles(pluginDir, "*.ffplugin", SearchOption.AllDirectories))
            {
                Logger.Instance?.ILog("Plugin file found: " + ffplugin);
                try
                {
                    using var zf = System.IO.Compression.ZipFile.Open(ffplugin, System.IO.Compression.ZipArchiveMode.Read);
                    var entry = zf.GetEntry(".plugininfo");
                    if (entry == null)
                        continue;
                    using var sr = new StreamReader(entry.Open());
                    string json = sr.ReadToEnd();

                    PluginInfo pi = JsonSerializer.Deserialize<PluginInfo>(json, options);

                    var plugin = dbPluginInfos.FirstOrDefault(x => x.Assembly == pi.Assembly);
                    bool hasSettings = false; // todo pi.plugin == null ? false : FormHelper.GetFields(plugin.GetType(), new Dictionary<string, object>()).Any();

                    bool isNew = plugin == null;
                    plugin ??= new();
                    installed.Add(pi.Assembly);
                    plugin.Version = pi.Version;
                    plugin.DateModified = DateTime.UtcNow;
                    plugin.HasSettings = hasSettings;
                    plugin.Deleted = false;
                    plugin.Fields = pi.Fields;
                    plugin.Elements = pi.Elements;

                    if (isNew == false)
                    {
                        if (plugin.Version == pi.Version && plugin.Deleted == false)
                            continue;
                        controller.Update(plugin).Wait();
                    }
                    else
                    {
                        // new dll
                        Logger.Instance.ILog("Adding new plugin: " + pi.Name + ", " + pi.Assembly);
                        plugin.Name = pi.Name;
                        plugin.Assembly = pi.Assembly;
                        plugin.DateCreated = DateTime.UtcNow;
                        plugin.DateModified = DateTime.UtcNow;
                        plugin.HasSettings = hasSettings;
                        plugin.Enabled = true;
                        plugin.Uid = Guid.NewGuid();
                        controller.Update(plugin).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Failed to scan for plugins: " + ex.Message);
                }
            }

            foreach (var dll in dbPluginInfos.Where(x => installed.Contains(x.Assembly) == false))
            {
                Logger.Instance.DLog("Missing plugin dll: " + dll.Assembly);
                dll.Deleted = true;
                dll.DateModified = DateTime.UtcNow;
                controller.Update(dll).Wait();
            }

            Logger.Instance.ILog("Finished scanning for plugins");
        }
    }
}
