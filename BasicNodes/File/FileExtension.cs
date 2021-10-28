namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlow.Plugins;
    using FileFlow.Plugins.Attributes;

    public class FileExtension : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 2;

        [StringArray(1)]
        public string[] Extensions { get; set; }
    }
}