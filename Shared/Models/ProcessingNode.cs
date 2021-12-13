namespace FileFlows.Shared.Models
{
    public class ProcessingNode: FileFlowObject
    {
        public string LoggingPath { get; set; }
        public string TempPath { get; set; }

        public bool Enabled { get; set; }

        public int Threads { get; set; }

    }
}
