namespace FileFlows.Shared.Models
{
    using System;
    using System.Collections.Generic;

    public class Flow : ViObject
    {
        public bool Enabled { get; set; }
        public string Description { get; set; }

        public string Template { get; set; }
        public List<FlowPart> Parts { get; set; }
    }
}