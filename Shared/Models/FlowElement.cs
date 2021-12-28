namespace FileFlows.Shared.Models
{
    using System.Collections.Generic;
    using System.Dynamic;
    using FileFlows.Plugin;

    public class FlowElement
    {
        public string Uid { get; set; }
        public string Name { get; set; }

        public string Icon { get; set; }
        public Dictionary<string, object> Variables { get; set; }

        public int Inputs { get; set; }
        public int Outputs { get; set; }
        public FlowElementType Type { get; set; }

        public string Group { get; set; }

        public List<ElementField> Fields { get; set; }

        public ExpandoObject Model { get; set; }
    }
}