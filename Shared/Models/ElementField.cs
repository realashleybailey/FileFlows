namespace FileFlow.Shared.Models
{
    using System.Collections.Generic;
    using System.Dynamic;
    using FileFlow.Plugins;
    public class ElementField
    {
        public int Order { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public FormInputType InputType { get; set; }

        public Dictionary<string, object> Parameters { get; set; }
    }

}