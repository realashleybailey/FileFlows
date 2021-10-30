namespace FileFlow.Plugin
{
    public class NodeParameters
    {
        public string FileName { get; set; }

        public ILogger Logger { get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }


    public enum NodeResult
    {
        Failure = 0,
        Success = 1,
    }
}