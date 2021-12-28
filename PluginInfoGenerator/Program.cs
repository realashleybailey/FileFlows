using FileFlows.Plugin;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace PluginInfoGenerator // Note: actual namespace depends on the project name.
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string dir = args.FirstOrDefault();
            if(string.IsNullOrEmpty(dir))
            {
                Console.WriteLine("No plugin directory defined");
                Environment.ExitCode = 1;
                return;
            }
            dir = new DirectoryInfo(dir).FullName;
            Console.WriteLine("Scanning directory: " + dir);

            var plugins = ScanForPlugins(dir);
            Console.WriteLine("Plugins found: " + plugins.Count);
            foreach (var plugin in plugins)
            {
                Console.WriteLine("Saving info for plugin: " + plugin.PackageName);
                string file = plugin.PackageName + ".plugininfo";
                plugin.PackageName = new FileInfo(plugin.PackageName).Name.Replace(".dll", "");
                string json = JsonSerializer.Serialize(plugin, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new PluginInfoConverter() }
                });
                Console.WriteLine("File: " + file);
                File.WriteAllText(file, json);
            }
            Console.WriteLine("Finished");
        }

        public static List<PluginInfo> ScanForPlugins(string pluginDir)
        {
            List<PluginInfo> results = new List<PluginInfo>();
            foreach (var dll in new DirectoryInfo(pluginDir).GetFiles("*.dll", SearchOption.AllDirectories))
            {
                Console.WriteLine("Checking dll: " + dll.FullName);
                try
                {
                    var test = new Node();
                    //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                    var assembly = Assembly.LoadFrom(dll.FullName);
                    Console.WriteLine("Getting types");
                    var types = assembly.GetTypes();
                    Console.WriteLine("Got types");
                    var pluginType = types.Where(x => x.IsAbstract == false && typeof(IPlugin).IsAssignableFrom(x)).FirstOrDefault();
                    if (pluginType == null)
                    {
                        Console.WriteLine("Plugin type not found in dll: " + dll.FullName);
                        continue;
                    }

                    var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                    var info = new PluginInfo();
                    info.PackageName = dll.FullName;
                    var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dll.FullName);
                    if (fvi == null)
                        continue;
                    info.Name = fvi.ProductName ?? dll.Name;
                    info.Version = fvi.FileVersion.ToString();
                    info.Elements = GetElements(assembly);
                    results.Add(info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
            return results;
        }

        static IEnumerable<Type> GetAssemblyNodeTypes(Assembly assembly)
        {
            try
            {
                var tNode = typeof(Node);
                var nodeTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(tNode) && x.IsAbstract == false);
                return nodeTypes;
            }
            catch (Exception) { }
            return new List<Type>();
        }

        private static List<FlowElement> GetElements(Assembly assembly)
        {
            var nodeTypes = GetAssemblyNodeTypes(assembly);
            List<FlowElement> elements = new List<FlowElement>();
            foreach (var x in nodeTypes)
            {
                FlowElement element = new FlowElement();
                element.Group = x.Namespace.Substring(x.Namespace.LastIndexOf(".") + 1);
                string typeName = x.Name;
                element.Name = typeName;
                element.Uid = x.FullName;
                element.Fields = new();
                var instance = (Node)Activator.CreateInstance(x);
                element.Inputs = instance.Inputs;
                element.Outputs = instance.Outputs;
                element.Type = instance.Type;
                element.Icon = instance.Icon;
                element.Variables = instance.Variables;

                var model = new ExpandoObject(); ;
                var dict = (IDictionary<string, object>)model;
                element.Model = model;

                element.Fields = FormHelper.GetFields(x, dict);

                elements.Add(element);
            }
            return elements.OrderBy(x => x.Group).ThenBy(x => x.Type).ThenBy(x => x.Name).ToList();
        }
    }
}