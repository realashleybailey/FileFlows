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
            var dbPluginInfos = controller.GetDataList().Result;

            List<string> installed = new List<string>();
            var options = new JsonSerializerOptions
            {
                Converters = { new Shared.Json.ValidatorConverter() }
            };

            List<string> langFiles = new List<string>();

            foreach (string ffplugin in Directory.GetFiles(pluginDir, "*.ffplugin", SearchOption.AllDirectories))
            {
                Logger.Instance?.ILog("Plugin file found: " + ffplugin);
                try
                {
                    using var zf = System.IO.Compression.ZipFile.Open(ffplugin, System.IO.Compression.ZipArchiveMode.Read);
                    var entry = zf.GetEntry(".plugininfo");
                    if (entry == null)
                    {
                        Logger.Instance?.WLog("Unable to find .plugininfo file");
                        continue;
                    }
                    using var sr = new StreamReader(entry.Open());
                    string json = sr.ReadToEnd();

                    var langEntry = zf.GetEntry("en.json");
                    if (langEntry != null)
                    {
                        using var srLang = new StreamReader(langEntry.Open());
                        langFiles.Add(srLang.ReadToEnd());
                    }

                    PluginInfo pi = JsonSerializer.Deserialize<PluginInfo>(json, options);

                    var plugin = dbPluginInfos.FirstOrDefault(x => x.Name == pi.Name);
                    bool hasSettings = false; // todo pi.plugin == null ? false : FormHelper.GetFields(plugin.GetType(), new Dictionary<string, object>()).Any();

                    bool isNew = plugin == null;
                    plugin ??= new();
                    installed.Add(pi.Name);
                    plugin.Version = pi.Version;
                    plugin.DateModified = DateTime.UtcNow;
                    plugin.HasSettings = hasSettings;
                    plugin.Deleted = false;
                    plugin.Fields = pi.Fields;
                    plugin.Elements = pi.Elements;

                    if (isNew == false)
                    {
                        Logger.Instance.ILog("Updating plugin: " + pi.Name);
                        controller.Update(plugin).Wait();
                    }
                    else
                    {
                        // new dll
                        Logger.Instance.ILog("Adding new plugin: " + pi.Name);
                        plugin.Name = pi.Name;
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
                    Logger.Instance?.ELog($"Failed to scan plugin {ffplugin}: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }

            foreach (var plugin in dbPluginInfos.Where(x => installed.Contains(x.Name) == false))
            {
                Logger.Instance.DLog("Missing plugin dll: " + plugin.Name);
                plugin.Deleted = true;
                plugin.DateModified = DateTime.UtcNow;
                controller.Update(plugin).Wait();
            }

            CreateLanguageFile(langFiles);

            Logger.Instance.ILog("Finished scanning for plugins");
        }


        static void CreateLanguageFile(List<string> jsonFiles)
        {
            var json = "{}";
            try
            {
                foreach (var jf in jsonFiles)
                {
                    try
                    {
                        string updated = JsonHelper.Merge(json, jf);
                        json = updated;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Error loading plugin json[0]:" + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error loading plugin json[1]:" + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            string dir = Path.Combine(Program.GetAppDirectory(), "wwwroot/i18n");
            if(Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "plugins.en.json"), json);        
        }
    }
}
