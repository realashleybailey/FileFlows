namespace ViWatcher.Shared.Models
{
    public class FlowElement:ViObject
    {
        public virtual int Inputs{ get; set; }
        public virtual int Outputs{ get; set; }
        public virtual FlowElementType Type { get; set; }

        public virtual string Group{ get; set; }

        public virtual int MaxInputs { get => -1; }
        public virtual int MaxOutputs { get => -1; }
        public virtual int MinInputs { get => -1; }
        public virtual int MinOutputs { get => -1; }
    }

}