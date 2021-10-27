namespace ViWatcher.Shared.Nodes {

    public interface IInputNode
    {
    }
    public interface IOutputNode
    {
        int Outputs{ get; }
    }
    public interface IConfigurableOutputNode:IOutputNode
    {
        int Outputs{ get; set; }
    }
}