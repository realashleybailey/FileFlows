namespace FileFlow.VideoNodes
{
    using System.ComponentModel;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class VideoFile : VideoNode
    {
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Input;

        public override int Execute(NodeParameters args)
        {
            string ffmpegExe = GetFFMpegExe(args);
            if (string.IsNullOrEmpty(ffmpegExe))
                return -1;

            try
            {

                var videoInfo = new VideoInfoHelper(ffmpegExe, args.Logger).Read(args.WorkingFile);
                if (videoInfo.VideoStreams.Any() == false)
                {
                    args.Logger.ILog("No video streams detected.");
                    return 0;
                }
                foreach (var vs in videoInfo.VideoStreams)
                {
                    args.Logger.ILog($"Video stream '{vs.Codec}' '{vs.Index}'");
                }



                foreach (var vs in videoInfo.AudioStreams)
                {
                    args.Logger.ILog($"Audio stream '{vs.Codec}' '{vs.Index}' '{vs.Language}");
                }

                SetVideoInfo(args, videoInfo);

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