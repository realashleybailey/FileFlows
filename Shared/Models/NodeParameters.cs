namespace ViWatcher.Shared.Models
{
    public class NodeParameters 
    {
        public string FileName{ get; set; }

        public IFlowLogger Logger{ get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;
    }
}