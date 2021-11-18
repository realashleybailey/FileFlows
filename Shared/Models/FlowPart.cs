namespace FileFlows.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using FileFlows.Plugin;

    public class FlowPart
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public string FlowElementUid { get; set; }
        public float xPos { get; set; }
        public float yPos { get; set; }

        public string Icon { get; set; }

        public int Inputs { get; set; }
        public int Outputs { get; set; }

        public List<FlowConnection> OutputConnections { get; set; }

        public FlowElementType Type { get; set; }

        public ExpandoObject Model { get; set; }
    }

    public class FlowConnection
    {
        public int Input { get; set; }
        public int Output { get; set; }
        public Guid InputNode { get; set; }
    }
}