namespace ViWatcher.BasicNodes.Video
{
    using System.ComponentModel;
    using ViWatcher.Shared.Attributes;
    using ViWatcher.Shared.Nodes;

    public class VideoCodec : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 2;

        [StringArray(1)]
        public string[] Codecs { get; set; }
    }
}