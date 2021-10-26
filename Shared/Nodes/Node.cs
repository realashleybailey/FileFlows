namespace ViWatcher.Shared.Nodes {

    public class Node
    {
        public int Inputs{ get; set; }
        public int Outputs{ get; set; }
        
        public FlowElementType Type { get; set; }
        
        public string Name => base.GetType().FullName.Substring("ViWatcher.Shared.Nodes.".Length);

        public string Group 
        {
            get
            {
                var type = base.GetType();
                string group = type.FullName.Substring(0, type.FullName.LastIndexOf("."));
                return group.Substring(group.LastIndexOf(".") + 1);
            }
        }
    }
}