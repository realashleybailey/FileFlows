The FFMPEG Builder is part of the Video Nodes plugin.

This lets the user break down a complex FFMPEG command into easy to configure nodes and will build up a command that can be executed at once.

This allows the user to for example convert video to HEVC, add an AC3 audio track, copy all subtitles and normalize the audio.

The FFMPEG builder will parse the Video File and build a list of all streams in a file.  If a stream has not been explicitly set to delete, or a codec conversion has been set, that stream will by default be copied to the new file.

So that means, if you have not done anything with subtitles, for example, all the subtitles from the original file will be copied to the output file.

***

### FFMPEG Builder: Start
![image](https://user-images.githubusercontent.com/958400/164885933-774d0672-7a3b-4033-8026-e93e2f819748.png)

The FFMPEG Build must first begin with the "FFMPEG Builder: Start" node.   This node is what constructs the builder and parses the video information for the builder.

***

### FFMPEG Builder: Executor
![image](https://user-images.githubusercontent.com/958400/164885987-a387639e-e5ee-4c6b-9a71-eddffe291d91.png)

The final part of the FFMPEG Builder is the "FFMPEG Builder: Executor" this is what takes the FFMPEG Builder, constructs into into a command for FFMPEG and executes it.
If will not execute, output 2, if nothing has been detected as needing to be executed.   If will return an output of -1 if the execution fails and the flow will exit.


