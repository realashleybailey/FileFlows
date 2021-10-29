namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class RenameFile : Node
    {
        public override int Outputs => 1;
    }
}