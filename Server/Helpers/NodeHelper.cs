namespace ViWatcher.Server.Helpers
{
    using System.Collections;
    using System.Reflection;
    using ViWatcher.Shared.Models;   
    using ViWatcher.Plugins;
    
    public class NodeHelper 
    {
        static List<Type> _NodeTypes;

        public static List<Type> NodeTypes 
        { 
            get 
            {
                if(_NodeTypes == null)
                    _NodeTypes = GetNodes();
                return _NodeTypes;
            } 
        }

        static List<Type> GetNodes()
        {
            List<Type> nodes = new List<Type>();
            var tNode = typeof(Node);
            foreach (var file in new DirectoryInfo("Plugins").GetFiles("*.dll"))
            {
                if (file.Name == "Shared")
                    continue;
                try
                {
                    var assembly = Assembly.LoadFile(file.FullName);
                    var nodeTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(tNode));
                    nodes.AddRange(nodeTypes);
                }
                catch (Exception) { }
            }
            return nodes;
        }

        public static Node LoadNode(FlowPart part)
        {
            var nt = NodeTypes.Where(x => x.FullName == part.FlowElementUid).FirstOrDefault();
            if(nt ==null)
                return new Node();
            var node = Activator.CreateInstance(nt);
            if (part.Model is IDictionary<string, object> dict)
            {
                foreach (var k in dict.Keys)
                {
                    try{
                        var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                        if(prop == null)
                            continue;

                        if(dict[k] == null)
                            continue;
                            
                        var value = ViWatcher.Shared.Converter.ConvertObject(prop.PropertyType, dict[k]);
                        prop.SetValue(node, value);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            return (Node)node;
        }
    }
}