using FileFlows.Server.Controllers;
using FileFlows.Shared.Models;
using System.Text.Json;

namespace FileFlows.Server.Helpers
{
    public class PluginScanner
    {
        public static string GetPluginDirectory() => Path.Combine(Program.GetAppDirectory(), "Plugins");

        public static void Scan()
        {
            Logger.Instance.DLog("Scanning for plugins");
            var pluginDir = GetPluginDirectory();
            Logger.Instance.DLog("Plugin path:" + pluginDir);

            if (Program.Docker)
                EnsureDefaultsExist(pluginDir);

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
                Logger.Instance?.DLog("Plugin file found: " + ffplugin);
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

                    if (string.IsNullOrEmpty(json))
                    {
                        Logger.Instance?.WLog("Unable to read plugininfo from file: " + ffplugin);
                        continue;
                    }

                    var langEntry = zf.GetEntry("en.json");
                    if (langEntry != null)
                    {
                        using var srLang = new StreamReader(langEntry.Open());
                        langFiles.Add(srLang.ReadToEnd());
                    }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    PluginInfo pi = JsonSerializer.Deserialize<PluginInfo>(json, options);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (pi == null)
                    {
                        Logger.Instance?.WLog("Unable to parse plugininfo from file: " + ffplugin);
                        continue;
                    }

                    var plugin = dbPluginInfos.FirstOrDefault(x => x.Name == pi.Name);
                    bool isNew = plugin == null;
                    plugin ??= new();
                    installed.Add(pi.Name);
                    plugin.PackageName = pi.PackageName;
                    plugin.Version = pi.Version;
                    plugin.DateModified = DateTime.Now;
                    plugin.Deleted = false;
                    plugin.Elements = pi.Elements;
                    plugin.Authors = pi.Authors;
                    plugin.Url = pi.Url;
                    plugin.Description = pi.Description;
                    plugin.Settings = pi.Settings;
                    plugin.HasSettings = pi.Settings?.Any() == true;

                    Logger.Instance.DLog("Plugin.Name: " + plugin.Name);
                    Logger.Instance.DLog("Plugin.PackageName: " + plugin.PackageName);
                    Logger.Instance.DLog("Plugin.Version: " + plugin.Version);
                    Logger.Instance.DLog("Plugin.Url: " + plugin.Url);
                    Logger.Instance.DLog("Plugin.Authors: " + plugin.Authors);

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
                        plugin.DateCreated = DateTime.Now;
                        plugin.DateModified = DateTime.Now;
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
                if (String.IsNullOrEmpty(plugin.PackageName))
                {
                    Logger.Instance.DLog("Delete old plugin: " + plugin.Name);
                    // its an old style plugin, perm delete it
                    controller.Delete(new ReferenceModel { Uids = new[] { plugin.Uid } }).Wait();
                }
                else
                {
                    Logger.Instance.DLog("Missing plugin: " + plugin.Name);
                    // mark as deleted.
                    plugin.Deleted = true;
                    plugin.DateModified = DateTime.Now;
                    controller.Update(plugin).Wait();
                }
            }

            CreateLanguageFile(langFiles);

            Logger.Instance.ILog("Finished scanning for plugins");
        }

        private static void EnsureDefaultsExist(string pluginDir)
        {
            Logger.Instance.ILog("PluginScanner: Ensuring default plugins exist: " + pluginDir);
            DirectoryInfo di = new DirectoryInfo(pluginDir);
            FileHelper.CreateDirectoryIfNotExists(di.FullName);

            var rootPlugins = new DirectoryInfo(Path.Combine(di.Parent.Parent.FullName, "Plugins"));

            if (rootPlugins.Exists == false)
            {
                Logger.Instance?.ILog("PluginScanner: Root plugin directory not found: " + rootPlugins);
                return;
            }
            var pluginFiles = rootPlugins.GetFiles("*.ffplugin");
            Logger.Instance?.ILog($"PluginScanner: Root plugins found: {pluginFiles.Length} in: {rootPlugins.FullName}");
            foreach (var file in pluginFiles)
            {
                Logger.Instance?.ILog($"PluginScanner: Root plugin: {file.FullName}");
                string dest = Path.Combine(pluginDir, file.Name);

                if (File.Exists(dest))
                {
                    // make sure the existing plugin is not newer than the docker plugin
                    var existing = GetFFPluginVersion(dest);
                    var dockerVersion = GetFFPluginVersion(file.FullName);

                    if(existing >= dockerVersion)
                    {
                        Logger.Instance?.DLog("PluginScanner: Existing plugin newer than docker plugin: " + file.Name);
                        continue;
                    }
                }

                Logger.Instance?.ILog("PluginScanner: Restoring default plugin: " + file.Name);
                file.CopyTo(dest, true);
            }
        }

        private static Version GetFFPluginVersion(string ffplugin)
        {
            if (File.Exists(ffplugin) == false)
                return new Version();
            try
            {
                using var zf = System.IO.Compression.ZipFile.Open(ffplugin, System.IO.Compression.ZipArchiveMode.Read);
                var entry = zf.GetEntry(".plugininfo");
                if (entry == null)
                {
                    Logger.Instance?.WLog("PluginScanner: Unable to find .plugininfo file");
                    return new Version();
                }
                using var sr = new StreamReader(entry.Open());
                string json = sr.ReadToEnd();

                if (string.IsNullOrEmpty(json))
                {
                    Logger.Instance?.WLog("PluginScanner: Unable to read plugininfo from file: " + ffplugin);
                    return new Version();
                }
                var options = new JsonSerializerOptions
                {
                    Converters = { new Shared.Json.ValidatorConverter() }
                };

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                PluginInfo pi = JsonSerializer.Deserialize<PluginInfo>(json, options);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                return Version.Parse(pi.Version);
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("PluginScanner: Failed to get plugin version: " + ex.Message + Environment.NewLine + ex.StackTrace);
                return new Version();
            }
        }

        internal static bool UpdatePlugin(string packageName, byte[] data)
        {
            try
            {
                string dest = Path.Combine(GetPluginDirectory(), packageName);
                if (dest.EndsWith(".ffplugin") == false)
                    dest += ".ffplugin";

                // save the plugin
                File.WriteAllBytes(dest, data);
                Logger.Instance.ILog("PluginScanner: Saving plugin : " + dest);

                // rescan for plugins
                Scan();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("Failed updating plugin: " + ex.Message + Environment.NewLine + ex.StackTrace);
                return false;
            }
        }

        internal static void Delete(string packageName)
        {
            string file = Path.Combine(GetPluginDirectory(), packageName + ".ffplugin");
            if(File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
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

            string dir = Program.GetAppDirectory();
            if (Program.Docker)
                dir = new DirectoryInfo(dir).Parent.FullName;
            dir = Path.Combine(dir, "wwwroot/i18n");

            if(Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "plugins.en.json"), json);        
        }
    }
}
