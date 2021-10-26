namespace ViWatcher.Shared.Models.FlowElements
{
    using System;
    using System.Collections.Generic;
    using ViWatcher.Shared.Attributes;
    using ViWatcher.Shared.Models;

    public class Function:FlowElement
    {
        [Code]
        public string Code{ get; set; }

        public override string Group { get => base.Group; set => base.Group = "value"; }
    }
}