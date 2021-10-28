using System.Reflection;
using FileFlow.Plugins;
using FileFlow.Shared.Models;

namespace FileFlow.Server.Helpers
{
    public class PluginHelper
    {

        public static void ScanForPlugins()
        {
            var dllPluginInfo = GetPlugins();

            var dbPluginInfos = DbHelper.Select<PluginInfo>();

            List<string> installed = new List<string>();

            foreach (var dll in dllPluginInfo)
            {
                Logger.Instance.DLog("Found plugin dll: " + dll.Assembly);
                installed.Add(dll.Assembly);
                var existing = dbPluginInfos.FirstOrDefault(x => x.Assembly == dll.Assembly);
                if (existing != null)
                {
                    if (existing.Version == dll.Version && existing.Deleted == false)
                        continue;
                    existing.Version = dll.Version;
                    existing.DateModified = DateTime.Now;
                    existing.Deleted = false;
                    DbHelper.Update(existing);
                }
                else
                {
                    // new dll
                    Logger.Instance.ILog("Adding new plug: " + dll.Name + ", " + dll.Assembly);
                    dll.DateCreated = DateTime.Now;
                    dll.DateModified = DateTime.Now;
                    dll.Uid = Guid.NewGuid();
                    DbHelper.Update(dll);
                }
            }

            foreach (var dll in dbPluginInfos.Where(x => installed.Contains(x.Assembly) == false))
            {
                Logger.Instance.DLog("Missing plugin dll: " + dll.Assembly);
                dll.Deleted = true;
                dll.DateModified = DateTime.Now;
                DbHelper.Update(dll);
            }
        }

        public static Plugin GetPlugin(string assemblyName)
        {
            try
            {
                var dll = Assembly.LoadFile(new FileInfo("Plugins/" + assemblyName).FullName);
                var pluginType = dll.GetTypes().Where(x => x.IsAbstract == false && x.BaseType == typeof(Plugin)).FirstOrDefault();
                if (pluginType == null)
                    throw new Exception("Plugin type not found in dll");

                var plugin = (Plugin)Activator.CreateInstance(pluginType);
                return plugin;
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error getting plugin: " + assemblyName + ", error: " + ex.Message);
                return default(Plugin);
            }
        }

        public static List<PluginInfo> GetPlugins()
        {
            List<PluginInfo> results = new List<PluginInfo>();
            foreach (var dll in new DirectoryInfo("Plugins").GetFiles("*.dll"))
            {
                Logger.Instance.DLog("Checking dll: " + dll.Name);
                try
                {
                    var assembly = Assembly.LoadFile(dll.FullName);
                    var types = assembly.GetTypes();
                    var pluginType = types.Where(x => x.IsAbstract == false && x.BaseType == typeof(Plugin)).FirstOrDefault();
                    if (pluginType == null)
                    {
                        Logger.Instance.DLog("Plugin type not found in dll: " + dll.Name);
                        foreach (var type in types)
                        {
                            Logger.Instance.DLog("type: " + type.Name + ", " + type.BaseType?.Name);
                        }
                        continue;
                    }
                    var plugin = (Plugin)Activator.CreateInstance(pluginType);
                    var info = new PluginInfo();
                    info.Assembly = dll.Name;
                    var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dll.FullName);
                    info.Version = fvi.FileVersion.ToString();
                    info.Name = plugin.Name;
                    results.Add(info);
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
            return results;
        }
    }
}