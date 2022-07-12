---
title: Video Nodes > Video File
permalink: /plugins/video-nodes/video-file
name: Video File
layout: default
plugin: Video Nodes
---

{% include node.html outputs=1 icon="fas fa-video" name="Video File" type="Input" %}

Video File is an input node which will scan a file as its discovered by the library scanner and load the Video Information (codecs/tracks etc) for processing in the flow.

This information is stored in the args.Parameters["VideoInfo"]

## Variables

| Variable | Description | Type | Example |
| :---: | :---: | :---: | :---: |
| vi.VideoInfo | VideoInfo object | object | See Below |
| vi.Width | Width of video | number | 1920 |
| vi.Height | Height of video | number | 1080 |
| vi.Duration | Duration of video in seconds | number | 60 |
| vi.Video.Codec | Codec of video | string | hevc |
| vi.Audio.Codec | Codec of audio | string | eac3 |
| vi.Audio.Channels | Number of audio channels of first audio track | number | 5.1 |
| vi.Audio.Language | Language of audio of first audio track | string | en |
| vi.Audio.Codecs | List of all audio of audio tracks | string | eac3, ac3, aac, dts |
| vi.Audio.Languages | List of all of audio track languages | string | en, deu, mao |
| vi.Resolution | Computed resolution of file (4K, 1080p, 720p, 480p, SD) | string | 4K |


## VideoInfo Object

```cs 
class VideoInfo
{
    public string FileName { get; set; }
    public float Bitrate { get; set; }
    public List<VideoStream> VideoStreams { get; set; } = new List<VideoStream>();
    public List<AudioStream> AudioStreams { get; set; } = new List<AudioStream>();
    public List<SubtitleStream> SubtitleStreams { get; set; } = new List<SubtitleStream>();

    public List<Chapter> Chapters { get; set; } = new List<Chapter>();
}


    public class VideoFileStream
    {
        /// <summary>
        /// The original index of the stream in the overall video
        /// </summary> 
        public int Index { get; set; }
        /// <summary>
        /// The index of the specific type
        /// </summary>
        public int TypeIndex { get; set; }
        /// <summary>
        /// The stream title (name)
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// The bitrate(BPS) of the video stream in bytes per second
        /// </summary>
        public float Bitrate { get; set; }

        /// <summary>
        /// The codec of the stream
        /// </summary>
        public string Codec { get; set; } = "";

        /// <summary>
        /// If this stream is an image
        /// </summary>
        public bool IsImage { get; set; }

        /// <summary>
        /// Gets or sets the index string of this track
        /// </summary>
        public string IndexString { get; set; }

        /// <summary>
        /// Gets or sets if the stream is HDR
        /// </summary>
        public bool HDR { get; set; }

        /// <summary>
        /// Gets or sets the input file index
        /// </summary>
        public int InputFileIndex { get; set; } = 0;
    }

    public class VideoStream : VideoFileStream
    {
        /// <summary>
        /// The width of the video stream
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The height of the video stream
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// The number of frames per second
        /// </summary>
        public float FramesPerSecond { get; set; }

        /// <summary>
        /// The duration of the stream
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    public class AudioStream : VideoFileStream
    {
        /// <summary>
        /// The language of the stream
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The channels of the stream
        /// </summary>
        public float Channels { get; set; }

        /// <summary>
        /// The duration of the stream
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The sample rate of the audio stream
        /// </summary>
        public int SampleRate { get; set; }
    }

    public class SubtitleStream : VideoFileStream
    {
        /// <summary>
        /// The language of the stream
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// If this is a forced subtitle
        /// </summary>
        public bool Forced { get; set; }
    }

    public class Chapter
    {
        public string Title { get; set; }    
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
```