namespace ViWatcher.Shared.Nodes.Functions
{
    using System.ComponentModel;
    using ViWatcher.Shared.Attributes;

    public class FileExtension : Node, IOutputNode
    {
        [DefaultValue(2)]
        public int Outputs{ get => 2; }

        [StringArray(1)]
        public string[] Extensions{ get; set; }
    }
}