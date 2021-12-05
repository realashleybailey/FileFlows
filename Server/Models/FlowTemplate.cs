using System.Dynamic;

namespace FileFlows.Server.Models
{
    class FlowTemplate
    {
        public string Name { get; set; }
        public List<FlowTemplatePart> Parts { get; set; }
    }

    class FlowTemplatePart
    {
        public string Node { get; set; }
        public Guid Uid { get; set; }
        public ExpandoObject Model { get; set; }

        public List<FlowTemplateConnection> Connections { get; set; }
    }

    class FlowTemplateConnection
    {
        public int Input { get; set; }
        public int Output { get; set; }
        public Guid Node { get; set; }
    }
}
