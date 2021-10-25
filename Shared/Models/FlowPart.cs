namespace ViWatcher.Shared.Models
{
    using System;
    public class FlowPart:ViObject
    {
        public Guid FlowElementUid { get; set; }
        public float xPos{ get; set; }
        public float yPos { get; set; }

        public int Inputs { get; set; }
        public int Outputs{ get; set; }

        public FlowElementType Type { get; set; }
    }
}