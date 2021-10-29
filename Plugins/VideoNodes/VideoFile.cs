namespace FileFlow.VideoNodes
{
    using System.ComponentModel;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class VideoFile : Node
    {
        public override int Outputs => 1;
    }
}