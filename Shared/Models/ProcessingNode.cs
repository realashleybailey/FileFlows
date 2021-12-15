using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FileFlows.Shared.Models
{
    public class ProcessingNode: FileFlowObject
    {
        public string TempPath { get; set; }

        public string Address { get; set; }

        public bool Enabled { get; set; }

        public int FlowRunners { get; set; }

        public string SignalrUrl { get; set; }
        public List<KeyValuePair<string, string>> Mappings { get; set; }
        public string Schedule { get; set; }

        public string Map(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (Mappings != null && Mappings.Count > 0)
            {
                foreach (var mapping in Mappings)
                {
                    if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                        continue;
                    path = Regex.Replace(path, Regex.Escape(mapping.Key), mapping.Value, RegexOptions.IgnoreCase);
                }
            }
            return path;
        }
        public string UnMap(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (Mappings != null && Mappings.Count > 0)
            {
                foreach (var mapping in Mappings)
                {
                    if (string.IsNullOrEmpty(mapping.Value) || string.IsNullOrEmpty(mapping.Key))
                        continue;
                    path = Regex.Replace(path, Regex.Escape(mapping.Value), mapping.Key, RegexOptions.IgnoreCase);
                }
            }
            return path;
        }
    }
}
