namespace FileFlow.VideoNodes
{
    using System.Linq;
    using System.ComponentModel;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;
    using FFMpegCore;

    public class VideoCodec : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 2;
        public override FlowElementType Type => FlowElementType.Logic;

        [StringArray(1)]
        public string[] Codecs { get; set; }

        public override int Execute(NodeParameters args)
        {
            if (args.Parameters.ContainsKey("MediaInfo") == false)
            {
                args.Logger.WLog("No codec information loaded, use a 'VideoFile' node first");
                return -1;
            }
            var mediaInfo = args.Parameters["MediaInfo"] as IMediaAnalysis;
            if (mediaInfo == null)
            {
                args.Logger.WLog("Invalid MediaInfo found");
                return -1;
            }

            Codecs = Codecs?.Select(x => x.ToLower())?.ToArray() ?? new string[] { };
            if (Codecs.Length == 0)
            {
                args.Logger.WLog("No codecs defined");
                return -1;
            }

            var codec = mediaInfo.VideoStreams.FirstOrDefault(x => Codecs.Contains(x.CodecName.ToLower()));
            if (codec != null)
            {
                args.Logger.ILog($"Matching video codec found[{codec.Index}]: {codec.CodecName}");
                return 1;
            }

            var acodec = mediaInfo.AudioStreams.FirstOrDefault(x => Codecs.Contains(x.CodecName.ToLower()));
            if (acodec != null)
            {
                args.Logger.ILog($"Matching audio codec found[{acodec.Index}]: {acodec.CodecName}, language: {acodec.Language}");
                return 1;
            }

            // not found, execute 2nd outputacodec
            return 2;
        }
    }
}