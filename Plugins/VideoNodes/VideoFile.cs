namespace FileFlow.VideoNodes
{
    using System.ComponentModel;
    using FFMpegCore;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class VideoFile : VideoNode
    {
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Input;

        public override int Execute(NodeParameters args)
        {
            string ffmpegPath = GetFFMpegPath(args);
            if (string.IsNullOrEmpty(ffmpegPath))
                return -1;

            try
            {
                GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);

                var mediaInfo = FFProbe.Analyse(args.WorkingFile);
                if (mediaInfo.VideoStreams.Any() == false)
                {
                    args.Logger.ILog("No video streams detected.");
                    return 0;
                }
                foreach (var vs in mediaInfo.VideoStreams)
                {
                    args.Logger.ILog($"Video stream '{vs.CodecName}' '{vs.CodecTag}' '{vs.Index}'");
                }



                foreach (var vs in mediaInfo.AudioStreams)
                {
                    args.Logger.ILog($"Audio stream '{vs.CodecName}' '{vs.CodecTag}' '{vs.Index}' '{vs.Language}");
                }

                SetMediaInfo(args, mediaInfo);

                return 1;
            }
            catch (Exception ex)
            {
                args.Logger.ELog("Failed processing VideoFile: " + ex.Message);
                return -1;
            }
        }
    }


}