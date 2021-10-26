namespace ViWatcher.Shared.Nodes.Functions
{
    using System.ComponentModel;
    using ViWatcher.Shared.Attributes;

    public class InputFile : Node, IOutputNode
    {
        [DefaultValue(1)]
        public int Outputs{ get => 1; }
    }
}