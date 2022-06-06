---
name: Video Encode
layout: default
plugin: Video Nodes
---

This is a generic video encoding node which will encode a video using FFMPEG.

For codecs/paraemeters refer to the [FFMPEG documentation](https://ffmpeg.org/ffmpeg.html)

* Video Codec: The video codec to use, this is used to test if the video needs to be reencoded or it already is in this format.
* Video Codec Parameters: The actual parameters used to encode the video.  This can be different to the Video Codec for example the codec could be hevc, but the encoding parameters for hevc could be "hevc_nvenc -preset hq -crf 23" to encode using NVIDIA hardware encoding and the "hq" preset
* Audio Codec: The audio codec to use
* Language: What language audio tracks and subtitles to keep/use.

Outputs
* Output 1: Video was reencoded
* Output 2: Video was not reencoded as was already in the specified codecs