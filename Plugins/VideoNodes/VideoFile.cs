namespace FileFlow.VideoNodes
{
    using System.ComponentModel;
    using FFMpegCore;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class VideoFile : Node
    {
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Input;

        public override int Execute(NodeParameters args)
        {
            var ffmpegFilePath = @"C:\utils\ffmpeg\ffmpeg.exe";

            try
            {
                //GlobalFFOptions.Configure(new FFOptions { BinaryFolder = @"C:\utils\ffmpeg" });
                GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\utils\ffmpeg");

                var mediaInfo = FFProbe.Analyse(args.FileName);
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
                args.Parameters.Add("MediaInfo", mediaInfo);

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