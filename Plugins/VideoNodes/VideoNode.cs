namespace FileFlow.VideoNodes
{
    using FFMpegCore;
    using FileFlow.Plugin;
    public abstract class VideoNode : Node
    {
        protected string GetFFMpegPath(NodeParameters args)
        {
            string ffmpeg = args.GetToolPath("FFMpeg");
            if (string.IsNullOrEmpty(ffmpeg))
            {
                args.Logger.ELog("FFMpeg tool not found.");
                return "";
            }
            var fileInfo = new FileInfo(ffmpeg);
            if (fileInfo.Exists == false)
            {
                args.Logger.ELog("FFMpeg tool configured by ffmpeg.exe file does not exist.");
                return "";
            }
            return fileInfo.DirectoryName;
        }

        private const string MEDIA_INFO = "MediaInfo";
        protected void SetMediaInfo(NodeParameters args, IMediaAnalysis info)
        {
            if (args.Parameters.ContainsKey(MEDIA_INFO))
                args.Parameters[MEDIA_INFO] = info;
            else
                args.Parameters.Add(MEDIA_INFO, info);
        }
        protected IMediaAnalysis GetMediaInfo(NodeParameters args)
        {
            if (args.Parameters.ContainsKey("MediaInfo") == false)
            {
                args.Logger.WLog("No codec information loaded, use a 'VideoFile' node first");
                return null;
            }
            var result = args.Parameters[MEDIA_INFO] as IMediaAnalysis;
            if (result == null)
            {
                args.Logger.WLog("MediaInformation not found for file");
                return null;
            }
            return result;
        }
    }
}