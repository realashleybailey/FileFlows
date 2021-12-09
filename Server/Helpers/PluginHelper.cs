namespace FileFlows.Server.Helpers
{
    using System.Dynamic;
    using System.Reflection;
    using FileFlows.Plugin;
    using FileFlows.Server.Controllers;
    using FileFlows.Shared.Models;

    /// <summary>
    /// This class will allow hot reloading of an plugin assembly so they can be update
    /// This class should return nothing from a Plugin assembly and just common C# objects
    /// </summary>
    public class PluginHelper : IDisposable
    {
        //HostAssemblyLoadContext Context;
        public PluginHelper()
        {
            //  Context = new HostAssemblyLoadContext(GetPluginDirectory());
        }
        private static string GetPluginDirectory() => new DirectoryInfo("Plugins").FullName;


        public void ScanForPlugins()
        {
            var dllPluginInfo = GetPlugins();

            var controller = new PluginController();
            var dbPluginInfos = controller.GetDataList().Result;

            List<string> installed = new List<string>();

            foreach (var dll in dllPluginInfo)
            {
                Logger.Instance.DLog("Found plugin dll: " + dll.Assembly);
                installed.Add(dll.Assembly);
                try
                {
                    var plugin = GetPlugin(dll.Assembly);
                    //plugin.GetType().GetMethod("Init", BindingFlags.Public | BindingFlags.Instance).Invoke(plugin, new object[] { });
                    plugin.Init();
                    var existing = dbPluginInfos.FirstOrDefault(x => x.Assembly == dll.Assembly);
                    bool hasSettings = plugin == null ? false : FormHelper.GetFields(plugin.GetType(), new Dictionary<string, object>()).Any();
                    if (existing != null)
                    {
                        if (existing.Version == dll.Version && existing.Deleted == false)
                            continue;
                        existing.Version = dll.Version;
                        existing.DateModified = DateTime.UtcNow;
                        existing.HasSettings = hasSettings;
                        existing.Deleted = false;
                        controller.Update(existing).Wait();
                    }
                    else
                    {
                        // new dll
                        Logger.Instance.ILog("Adding new plug: " + dll.Name + ", " + dll.Assembly);
                        dll.DateCreated = DateTime.UtcNow;
                        dll.DateModified = DateTime.UtcNow;
                        dll.HasSettings = hasSettings;
                        dll.Enabled = true;
                        dll.Uid = Guid.NewGuid();
                        controller.Update(dll).Wait();
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
        }

        public List<string> GetPluginDirectories()
        {

            string pluginsDir = GetPluginDirectory();

            if (Directory.Exists(pluginsDir) == false)
                Directory.CreateDirectory(pluginsDir);

            List<string> results = new List<string>();
            foreach (var subdir in new DirectoryInfo(pluginsDir).GetDirectories())
            {
                var versionDir = subdir.GetDirectories().OrderByDescending(x =>
                {
                    if (Version.TryParse(x.Name, out Version v))
                        return v;
                    return new Version(0, 0);
                }).FirstOrDefault();

                if (versionDir == null)
                    continue;

                results.Add(versionDir.FullName);
            }
            return results;
        }
        private string GetPluginDirectory(string assemblyName)
        {

            string pluginsDir = GetPluginDirectory();

            if (Directory.Exists(pluginsDir) == false)
                Directory.CreateDirectory(pluginsDir);

            List<string> results = new List<string>();
            var dirInfo = new DirectoryInfo(pluginsDir + "/" + assemblyName.Replace(".dll", ""));
            if (dirInfo.Exists == false)
                return string.Empty;

            var versionDir = dirInfo.GetDirectories().OrderByDescending(x =>
            {
                if (Version.TryParse(x.Name, out Version v))
                    return v;
                return new Version(0, 0);
            }).FirstOrDefault();

            if (versionDir == null)
                return string.Empty;

            return versionDir.FullName;
        }

        /// <summary>
        /// Get info about installed plugins
        /// </summary>
        /// <returns></returns>
        public List<PluginInfo> GetPlugins()
        {
            List<PluginInfo> results = new List<PluginInfo>();
            foreach (var pluginDir in GetPluginDirectories())
            {
                foreach (var dll in new DirectoryInfo(pluginDir).GetFiles("*.dll"))
                {

                    Logger.Instance.DLog("Checking dll: " + dll.FullName);
                    try
                    {
                        var test = new Plugin.Node();
                        //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                        var assembly = Assembly.LoadFrom(dll.FullName);
                        Console.WriteLine("Getting types");
                        var types = assembly.GetTypes();
                        Console.WriteLine("Got types");
                        var pluginType = types.Where(x => x.IsAbstract == false && typeof(IPlugin).IsAssignableFrom(x)).FirstOrDefault();
                        if (pluginType == null)
                        {
                            Logger.Instance.DLog("Plugin type not found in dll: " + dll.FullName);
                            continue;
                        }

                        var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                        var info = new PluginInfo();
                        info.Assembly = dll.Name;
                        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dll.FullName);
                        info.Version = fvi.FileVersion.ToString();
                        //info.Name = plugin.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance).GetValue(plugin) as string ?? "";
                        info.Name = plugin.Name;
                        results.Add(info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog(ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            return results;
        }

        internal Dictionary<string, object> GetPartVariables(string flowElementUid)
        {
            var nt = GetNodeType(flowElementUid);
            if (nt == null)
                return new Dictionary<string, object>();
            var node = Activator.CreateInstance(nt) as Node;
            if(node?.Variables == null || node.Variables.Count == 0)
                return new Dictionary<string, object>();
            return node.Variables;
        }

        /// <summary>
        /// Gets the info for the default InputFile node
        /// </summary>
        /// <returns>the info for the default InputFile node</returns>
        internal (string name, string fullName) GetInputFileInfo()
        {
            string dir = GetPluginDirectory("BasicNodes");
            if(string.IsNullOrEmpty(dir))
                return (string.Empty, string.Empty);
            string assembglyFile = Path.Combine(dir, "BasicNodes.dll");
            var inputFileType = GetAssemblyNodeTypes(assembglyFile)?.Where(x => x.Name == "InputFile").FirstOrDefault();
            if (inputFileType == null)
                return (string.Empty, string.Empty);
            return new(inputFileType.Name, inputFileType.FullName);
        }

        internal IEnumerable<FlowElement> GetElements()
        {
            var nodeTypes = GetNodeTypes();
            List<FlowElement> elements = new List<FlowElement>();
            foreach (var x in nodeTypes)
            {
                FlowElement element = new FlowElement();
                element.Group = x.Namespace.Substring(x.Namespace.LastIndexOf(".") + 1);
                element.Name = x.Name;
                element.Uid = x.FullName;
                element.Fields = new();
                var instance = (Node)Activator.CreateInstance(x);
                element.Inputs = instance.Inputs;
                element.Outputs = instance.Outputs;
                element.Type = instance.Type;
                element.Icon = instance.Icon;

                var model = new ExpandoObject(); ;
                var dict = (IDictionary<string, object>)model;
                element.Model = model;

                element.Fields = FormHelper.GetFields(x, dict);

                elements.Add(element);
            }
            return elements.OrderBy(x => x.Group).ThenBy(x => x.Type).ThenBy(x => x.Name);
        }

        internal PluginInfo LoadPluginInfo(PluginInfo pi)
        {
            pi.Settings ??= new System.Dynamic.ExpandoObject();
            var dict = (IDictionary<string, object>)pi.Settings;

            var plugin = GetPlugin(pi.Assembly);

            if (plugin != null)
                pi.Fields = FormHelper.GetFields(plugin.GetType(), dict);
            return pi;
        }


        IPlugin GetPlugin(string assemblyName)
        {
            try
            {
                string dir = GetPluginDirectory(assemblyName);
                if (string.IsNullOrEmpty(dir))
                    return null;

                string filename = new FileInfo(dir + "/" + assemblyName).FullName;

                // var dll = Context.LoadFromAssemblyPath(filename);
                var dll = Assembly.LoadFrom(filename);
                var pluginType = dll.GetTypes().Where(x => x.IsAbstract == false && typeof(IPlugin).IsAssignableFrom(x)).FirstOrDefault();
                if (pluginType == null)
                    throw new Exception("Plugin type not found in dll");

                return (IPlugin)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error getting plugin: " + assemblyName + ", error: " + ex.Message);
                return default(IPlugin);
            }
        }

        public void Dispose()
        {
            // if (Context != null)
            // {
            //     Context.Unload();
            //     Context = null;
            // }
        }

        private Type? GetNodeType(string fullName)
        {
            var dirs = GetPluginDirectories();
            foreach (var dir in dirs)
            {
                foreach (var dll in new DirectoryInfo(dir).GetFiles("*.dll"))
                {
                    try
                    {
                        //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                        var assembly = Assembly.LoadFrom(dll.FullName);
                        var types = assembly.GetTypes();
                        var pluginType = types.Where(x => x.IsAbstract == false && x.FullName == fullName).FirstOrDefault();
                        if (pluginType != null)
                            return pluginType;
                    }
                    catch (Exception) { }
                }
            }
            return null;
        }

        private List<Type> GetNodeTypes()
        {
            List<Type> nodes = new List<Type>();
            var tNode = typeof(Node);
            var plugins= new PluginController().GetDataList().Result.Where(x => x.Deleted == false && x.Enabled);
            foreach (var plugin in plugins)
            {
                var nodeTypes = GetAssemblyNodeTypes(plugin.Assembly)?.ToList();
                if (nodeTypes?.Any() == true)
                    nodes.AddRange(nodeTypes);
            }
            return nodes;
        }

        IEnumerable<Type> GetAssemblyNodeTypes(string pluginAssembly)
        {
            try
            {
                var tNode = typeof(Node);
                string dll = pluginAssembly;
                if (File.Exists(dll) == false)
                {
                    string dir = GetPluginDirectory(pluginAssembly);
                    if (string.IsNullOrEmpty(dir))
                        return new List<Type>();

                    dll = new FileInfo(Path.Combine(dir, pluginAssembly)).FullName;
                }
                //var assembly = Context.LoadFromAssemblyPath(dll);
                var assembly = Assembly.LoadFrom(dll);
                var nodeTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(tNode) && x.IsAbstract == false);
                return nodeTypes;
            }
            catch (Exception) { }
            return new List<Type>();
        }

        /// <summary>
        /// This needs to return an instance so the FlowExecutor can use it...
        /// </summary>
        /// <param name="part">The flow part</param>
        /// <returns>an insstance of the plugin node</returns>
        public Node LoadNode(FlowPart part)
        {
            var nt = GetNodeType(part.FlowElementUid);
            if (nt == null)
                return new Node();
            var node = Activator.CreateInstance(nt);
            if (part.Model is IDictionary<string, object> dict)
            {
                foreach (var k in dict.Keys)
                {
                    try
                    {
                        if (k == "Name")
                            continue; // this is just the display name in the flow UI
                        var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                        if (prop == null)
                            continue;

                        if (dict[k] == null)
                            continue;

                        var value = FileFlows.Shared.Converter.ConvertObject(prop.PropertyType, dict[k]);
                        if (value != null)
                            prop.SetValue(node, value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Type: " + nt.Name + ", Property: " + k);
                        Logger.Instance.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            return (Node)node;
        }
    }
}