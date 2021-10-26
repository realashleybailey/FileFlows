namespace ViWatcher.Shared.Models
{
    using System;
    using System.Collections.Generic;

    public class FlowPart:ViObject
    {
        public Guid FlowElementUid { get; set; }
        public float xPos{ get; set; }
        public float yPos { get; set; }

        public int Inputs { get; set; }
        public int Outputs{ get; set; }

        public List<FlowConnection> OutputConnections { get; set; }

        public FlowElementType Type { get; set; }
    }

    public class FlowConnection {
        public int Input { get; set; }
        public int Output{ get; set; }
        public Guid InputNode{ get; set; }
    }
}