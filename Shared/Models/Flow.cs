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

    public class FlowTemplateModel
    {
        public Flow Flow { get; set; }
        public List<TemplateField> Fields { get; set; }
    }

    public class TemplateField
    {
        public Guid Uid { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public object Default { get; set; }
    }
}