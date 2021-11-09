namespace FileFlow.Plugin
{
    using System.Collections.Generic;
    public class NodeParameters
    {
        public string FileName { get; set; }

        public string OutputFile { get; set; }

        public string WorkingFile { get; set; }

        public ILogger Logger { get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public Func<string, string> GetToolPath { get; set; }

        public Action<float> PartPercentageUpdate { get; set; }
    }


    public enum NodeResult
    {
        Failure = 0,
        Success = 1,
    }
}