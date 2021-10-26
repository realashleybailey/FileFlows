namespace ViWatcher.Shared.Nodes {

    public interface IInputNode
    {
        int Inputs{ get; }
    }
    public interface IOutputNode
    {
        int Outputs{ get; }
    }
    public interface IConfigurableInputNode:IInputNode
    {
        int Inputs { get; set; }
    }
    public interface IConfigurableOutputNode:IOutputNode
    {
        int Outputs{ get; set; }
    }
}