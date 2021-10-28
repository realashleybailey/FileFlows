namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlow.Plugins;
    using FileFlow.Plugins.Attributes;

    public class RenameFile : Node
    {
        public override int Outputs => 1;
    }
}