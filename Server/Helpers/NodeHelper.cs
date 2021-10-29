namespace FileFlow.Server.Helpers
{
    using System.Collections;
    using System.Reflection;
    using FileFlow.Shared.Models;
    using FileFlow.Plugin;

    public class NodeHelper
    {
        static List<Type> _NodeTypes;

        public static List<Type> NodeTypes
        {
            get
            {
                if (_NodeTypes == null)
                    _NodeTypes = GetNodeTypes();
                return _NodeTypes;
            }
        }

        public static List<Type> GetNodeTypes()
        {
            List<Type> nodes = new List<Type>();
            var tNode = typeof(Node);
            var plugins = DbHelper.Select<PluginInfo>().Where(x => x.Deleted == false && x.Enabled);
            foreach (var plugin in plugins)
            {
                try
                {
                    var assembly = Assembly.LoadFile(new FileInfo(Path.Combine("Plugins", plugin.Assembly)).FullName);
                    var nodeTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(tNode));
                    nodes.AddRange(nodeTypes);
                }
                catch (Exception) { }
            }
            _NodeTypes = nodes;
            return nodes;
        }

        public static Node LoadNode(FlowPart part)
        {
            var nt = NodeTypes.Where(x => x.FullName == part.FlowElementUid).FirstOrDefault();
            if (nt == null)
                return new Node();
            var node = Activator.CreateInstance(nt);
            if (part.Model is IDictionary<string, object> dict)
            {
                foreach (var k in dict.Keys)
                {
                    try
                    {
                        var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                        if (prop == null)
                            continue;

                        if (dict[k] == null)
                            continue;

                        var value = FileFlow.Shared.Converter.ConvertObject(prop.PropertyType, dict[k]);
                        prop.SetValue(node, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            return (Node)node;
        }
    }
}