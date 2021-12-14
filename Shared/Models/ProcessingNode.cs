using System.Collections.Generic;

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
    }
}
