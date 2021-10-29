namespace FileFlow.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text.Json.Serialization;

    public class PluginInfo : ViObject
    {
        public bool Enabled { get; set; }
        public string Assembly { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
        public bool HasSettings { get; set; }
        public ExpandoObject Settings { get; set; }

        public List<ElementField> Fields { get; set; }
    }
}