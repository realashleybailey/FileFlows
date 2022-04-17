namespace FileFlows.Shared.Models
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text.RegularExpressions;
    using FileFlows.Plugin;

    public class FlowElement
    {
        public string Uid { get; set; }
        public string Name { get; set; }

        static readonly Regex rgxFormatLabel = new Regex("(?<=[A-Za-z])(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[0-9]?[A-Z])");

        private string _DisplayName;
        public string DisplayName
        {
            get {
                if (_DisplayName == null && Translater.InitDone)
                {
                    _DisplayName = FormatName(this.Name);
                }
                return _DisplayName;
            }
        }

        public static string FormatName(string name)
        {
            string translated = Translater.Instant($"Flow.Parts.{name}.Label", supressWarnings: true);
            if (string.IsNullOrEmpty(translated) == false && translated != "Label")
                return translated;

            name = name[(name.LastIndexOf(".") + 1)..];
            string dn = name.Replace("_", " ");
            dn = rgxFormatLabel.Replace(dn, " ");
            return dn;
        }

        public string Icon { get; set; }
        public Dictionary<string, object> Variables { get; set; }

        public int Inputs { get; set; }
        public int Outputs { get; set; }
        public string HelpUrl { get; set; }
        public FlowElementType Type { get; set; }

        public bool FailureNode { get; set; }

        public string Group { get; set; }

        public List<string> OutputLabels { get; set; }

        public List<ElementField> Fields { get; set; }

        public ExpandoObject Model { get; set; }
    }
}