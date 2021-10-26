namespace ViWatcher.Shared.Models
{
    using System;
    using System.Collections.Generic;

    public class Flow:ViObject
    {
        public List<FlowPart> Parts { get; set; }
    }
}