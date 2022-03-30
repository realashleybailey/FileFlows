using FileFlows.Plugin;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PluginInfoGenerator // Note: actual namespace depends on the project name.
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if (DEBUG)
            args = new[] { @"D:\src\FileFlows\FileFlowsPlugins\VideoNodes\bin\Debug\net6.0\VideoNodes.dll", @"D:\src\FileFlows\FileFlowsPlugins\VideoNodes\VideoNodes.csproj" };
#endif
            if(args.Length != 2)
            {
                Console.WriteLine("Must specify first the DLL then the csproj file");
                return;
            }

            var plugin = LoadPlugin(new FileInfo(args[0]), new FileInfo(args[1]));
            if(plugin == null)
            {
                Console.WriteLine("Failed to load plugin");
                return;
            }

            Console.WriteLine("Saving info for plugin: " + plugin.PackageName);
            string file = plugin.PackageName + ".plugininfo";
            string nfoFile = plugin.PackageName + ".nfo";
            plugin.PackageName = new FileInfo(plugin.PackageName).Name.Replace(".dll", "");
            string json = JsonSerializer.Serialize(plugin, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new ValidatorConverter(), new PluginInfoConverter() }
            });
            Console.WriteLine("File: " + file);
            File.WriteAllText(file, json);
            WriteBasicInfo(plugin, nfoFile);
            Console.WriteLine("Finished");
        }

        private static void WriteBasicInfo(PluginInfo plugin, string output)
        {
            PluginBasicInfo info = new PluginBasicInfo();
            info.Name = plugin.Name;
            info.Authors = plugin.Authors;
            info.Version = plugin.Version;
            info.Description = plugin.Description;
            info.Package = plugin.PackageName;
            info.Url = plugin.Url;
            info.MinimumVersion = plugin.MinimumVersion;
            info.Elements = plugin.Elements.Select(x => x.Name).ToArray();

            string json = JsonSerializer.Serialize(info, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine("Saving nfo file to: " + output);
            File.WriteAllText(output, json);
        }

        public static PluginInfo LoadPlugin(FileInfo dll, FileInfo csproj)
        {
            Console.WriteLine("Checking dll: " + dll);
            Console.WriteLine("CSProject: " + csproj);
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
                return null;
            }

            var plugin = (IPlugin)Activator.CreateInstance(pluginType);
            var info = new PluginInfo();
            info.PackageName = dll.FullName;
            info.MinimumVersion = plugin.MinimumVersion;
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dll.FullName);
            if (fvi == null)
                return null;
            info.Name = fvi.ProductName ?? dll.Name;
            info.Version = fvi.FileVersion.ToString();
            info.Elements = GetElements(assembly);
            info.Settings = GetSettings(assembly);

            string csProjFile = System.IO.File.ReadAllText(csproj.FullName);
            info.Authors = GetFromCsProject(csProjFile, "Authors");
            info.Description = GetFromCsProject(csProjFile, "Description");
            info.Url = GetFromCsProject(csProjFile, "PackageProjectUrl");

            return info;
        }

        private static string GetFromCsProject(string source, string attribute)
        {
            var match = Regex.Match(source, $"(?<=({attribute}>))[^<]+");
            if (match.Success)
                return match.Value;
            return String.Empty;
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
                element.HelpUrl = instance.HelpUrl;

                var model = new ExpandoObject(); ;
                var dict = (IDictionary<string, object>)model;
                element.Model = model;

                element.Fields = FormHelper.GetFields(x, dict);

                elements.Add(element);
            }
            return elements.OrderBy(x => x.Group).ThenBy(x => x.Type).ThenBy(x => x.Name).ToList();
        }


        private static List<ElementField> GetSettings(Assembly assembly)
        {
            var tSettings = typeof(IPluginSettings);
            var types = assembly.GetTypes().ToArray();
            var settingType = types.Where(x => tSettings.IsAssignableFrom(x) && x.IsAbstract == false).FirstOrDefault();
            if (settingType == null)
                return new List<ElementField>();

            var fields = FormHelper.GetFields(settingType, new Dictionary<string, object>());
            return fields;
        }
    }

    class PluginBasicInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string MinimumVersion { get; set; }
        public string Authors { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Package { get; set; }
        public string[] Elements { get; set; }
    }
}