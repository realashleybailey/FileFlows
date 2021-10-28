namespace ViWatcher.Plugins
{
    public class NodeParameters 
    {
        public string FileName{ get; set; }

        public ILogger Logger{ get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;
    }

    
    public enum NodeResult {
        Failure = 0,
        Success = 1,
    }
}