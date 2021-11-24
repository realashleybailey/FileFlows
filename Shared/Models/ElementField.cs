namespace FileFlows.Shared.Models
{
    using System.Collections.Generic;
    using System.Dynamic;
    using FileFlows.Plugin;
    public class ElementField
    {
        public int Order { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets optional place holder text, this can be a translation key
        /// </summary>
        public string Placeholder { get; set; }
        public FormInputType InputType { get; set; }

        public Dictionary<string, object> Variables { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        public List<Validators.Validator> Validators { get; set; }
    }

}