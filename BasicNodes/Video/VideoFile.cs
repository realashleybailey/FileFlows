namespace FileFlow.BasicNodes.Video
{
    using System.ComponentModel;
    using FileFlow.Plugins;
    using FileFlow.Plugins.Attributes;

    public class VideoFile : Node
    {
        public override int Outputs => 1;
    }
}