namespace FileFlow.VideoNodes
{
    using System.ComponentModel;
    using System.Text.RegularExpressions;
    using FFMpegCore;
    using FFMpegCore.Enums;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class VideoH265AC3 : VideoNode
    {
        public override int Outputs => 2;
        public override int Inputs => 1;
        public override FlowElementType Type => FlowElementType.Process;

        [DefaultValue("eng")]
        [Text(1)]
        public string Language { get; set; }

        [DefaultValue(21)]
        [NumberInt(2)]
        public int Crf { get; set; }
        [DefaultValue(true)]
        [Boolean(3)]
        public bool NvidiaEncoding { get; set; }
        [DefaultValue(0)]
        [NumberInt(4)]
        public int Threads { get; set; }

        public override int Execute(NodeParameters args)
        {
            try
            {
                IMediaAnalysis mediaInfo = GetMediaInfo(args);
                if (mediaInfo == null)
                    return -1;

                Language = Language?.ToLower() ?? "";

                // ffmpeg is one based for stream index, so video should be 1, audio should be 2

                var videoH265 = mediaInfo.VideoStreams.FirstOrDefault(x => Regex.IsMatch(x.CodecName ?? "", @"^(hevc|h(\.)?265)$", RegexOptions.IgnoreCase));
                var videoTrack = videoH265 ?? mediaInfo.VideoStreams[0];
                args.Logger.ILog("Video: ", videoTrack);

                var bestAudio = mediaInfo.AudioStreams.Where(x => System.Text.Json.JsonSerializer.Serialize(x).ToLower().Contains("commentary") == false)
                .OrderBy(x =>
                {
                    if (Language != string.Empty)
                    {
                        args.Logger.ILog("Language: " + x.Language, x);
                        if (string.IsNullOrEmpty(x.Language))
                            return 50; // no language specified
                        if (x.Language?.ToLower() != Language)
                            return 100; // low priority not the desired language
                    }
                    return 0;
                })
                .ThenByDescending(x => x.Channels)
                //.ThenBy(x => x.CodecName.ToLower() == "ac3" ? 0 : 1) // if we do this we can get commentary tracks...
                .ThenBy(x => x.Index)
                .FirstOrDefault();

                bool firstAc3 = bestAudio?.CodecName?.ToLower() == "ac3" && mediaInfo.AudioStreams[0] == bestAudio;
                args.Logger.ILog("Best Audio: ", (object)bestAudio ?? (object)"null");

                if (firstAc3 == true && videoH265 != null)
                {
                    args.Logger.DLog("File is h265 with the first audio track being AC3");
                    return 2;
                }

                string ffmpegPath = GetFFMpegPath(args);
                if (string.IsNullOrEmpty(ffmpegPath))
                    return -1;


                GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
                var ffmpeg = FFMpegArguments.FromFileInput(args.WorkingFile)
                                            .OutputToFile(args.OutputFile, true, options =>
                                            {
                                                if (NvidiaEncoding == false && Threads > 0)
                                                    options.WithCustomArgument($"-threads {Math.Min(Threads, 16)}");
                                                if (videoH265 == null)
                                                    options.WithCustomArgument($"-map 0:v:0 -c:v {(NvidiaEncoding ? "hevc_nvenc -preset hq" : "libx265")} -crf " + (Crf > 0 ? Crf : 21));
                                                else
                                                    options.WithCustomArgument($"-map 0:v:0 -c:v copy");

                                                if (bestAudio.CodecName.ToLower() != "ac3")
                                                    options.WithCustomArgument($"-map 0:{bestAudio.Index} -c:a ac3");
                                                else
                                                    options.WithCustomArgument($"-map 0:{bestAudio.Index} -c:a copy");

                                                if (Language != string.Empty)
                                                    options.WithCustomArgument($"-map 0:s:m:language:{Language}? -c:s copy");
                                                else
                                                    options.WithCustomArgument($"-map 0:s? -c:s copy");
                                            });

                args.Logger.ILog(new string('=', ("FFMpeg.Arguments: " + ffmpeg.Arguments).Length));
                args.Logger.ILog("FFMpeg.Arguments: " + ffmpeg.Arguments);
                args.Logger.ILog(new string('=', ("FFMpeg.Arguments: " + ffmpeg.Arguments).Length));


                //ffmpeg.NotifyOnProgress((time) => args.Logger.ILog($"INFO: Record: Progress: {time}"));
                ffmpeg.NotifyOnOutput((str, type) =>
                {
                    args.Logger.ILog($"INFO: Output: {str}");
                });
                ffmpeg.ProcessSynchronously();
                if (File.Exists(args.OutputFile))
                    args.WorkingFile = args.OutputFile;
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