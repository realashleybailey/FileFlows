namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlow.Plugins;
    using FileFlow.Plugins.Attributes;

    public class MoveFile : Node
    {
        public override int Outputs => 1;
    }
}