namespace ViWatcher.Shared.Nodes.Functions
{
    using ViWatcher.Shared.Attributes;

    public class Function : Node,  IConfigurableInputNode, IConfigurableOutputNode
    {
        [NumberIntAttribute(1)]
        public int Inputs{ get; set; }

        [NumberIntAttribute(2)]
        public int Outputs{ get; set; }
        
        [Code(3)]
        public string Code{ get; set; }
    }
}