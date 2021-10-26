namespace ViWatcher.Shared.Models
{
    using System.Collections.Generic;

    public class FlowElement
    {
        public string Uid{ get; set; }
        public string Name{ get; set; }

        public int Inputs{ get; set; }
        public int Outputs{ get; set; }
        public FlowElementType Type { get; set; }

        public string Group{ get; set; }

        public int MaxInputs { get => -1; }
        public int MaxOutputs { get => -1; }
        public int MinInputs { get => -1; }
        public int MinOutputs { get => -1; }

        public List<FlowElementField> Fields{ get; set; }
    }

    public class FlowElementField{
        public int Order{ get; set; }
        public string Type{ get; set; }
        public string Name{ get; set; }
        public FormInputType InputType{ get; set; }
    }

}