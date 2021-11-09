namespace FileFlow.VideoNodes
{
    using FileFlow.Plugin;
    public abstract class VideoNode : Node
    {
        protected string GetFFMpegExe(NodeParameters args)
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
            return fileInfo.FullName;
        }
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

        private const string VIDEO_INFO = "VideoInfo";
        protected void SetVideoInfo(NodeParameters args, VideoInfo info)
        {
            if (args.Parameters.ContainsKey(VIDEO_INFO))
                args.Parameters[VIDEO_INFO] = info;
            else
                args.Parameters.Add(VIDEO_INFO, info);
        }
        protected VideoInfo GetVideoInfo(NodeParameters args)
        {
            if (args.Parameters.ContainsKey(VIDEO_INFO) == false)
            {
                args.Logger.WLog("No codec information loaded, use a 'VideoFile' node first");
                return null;
            }
            var result = args.Parameters[VIDEO_INFO] as VideoInfo;
            if (result == null)
            {
                args.Logger.WLog("VideoInfo not found for file");
                return null;
            }
            return result;
        }
    }
}