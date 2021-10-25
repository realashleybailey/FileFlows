namespace ViWatcher.Shared.Models
{
    public class FlowElement:ViObject
    {
        public int Inputs{ get; set; }
        public int Outputs{ get; set; }
        public FlowElementType Type { get; set; }
    }

}