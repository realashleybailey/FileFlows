---
name: Audio Track Reorder
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

![image](https://user-images.githubusercontent.com/958400/164950111-dec71b15-3bcd-4bfb-a9c0-64ff9c8952d0.png)

This node will reorder the audio tracks given the parameters and update the order in "FFMPEG Builder".  When "FFMPEG Builder: Executor" is run the audio will be written in the order specified.

### Languages
A list of language codes used to prefer, anything not matching these will come afterward
![image](https://user-images.githubusercontent.com/958400/164950180-ba74f1a1-c660-46ff-ba79-fc20b0afdf8f.png)

### Ordered Audio Codecs
A list of codecs to order the tracks by.  This is done after Language.   
![image](https://user-images.githubusercontent.com/958400/164950192-d9dc38d8-4b90-4714-ae4f-c6392c5de2aa.png)

### Channels
The channel order to use, anything not matching these will be ordered afterward.
![image](https://user-images.githubusercontent.com/958400/164950200-f108a2bb-bb2c-4739-b7da-d47d7c1b125a.png)


### Outputs
1. Audio tracks have been reordered in "FFMPEG Builder"
2. Audio tracks have NOT been reordered

### Example
If you had this for example,
![image](https://user-images.githubusercontent.com/958400/164950206-fd34a308-facc-459c-8c5f-2eaa0e74d5b1.png)

The order will be
- English AC3 5.1
- English AC3 Stereo
- English AAC 5.1
- English AAC Stereo
- French AC3 5.1
- French AC3 Stereo
- French AAC 5.1
- French AAC Stereo
- German AC3 Stereo
- German AAC 5.1
- Dutch ACC Stereo

Where German and Dutch were not specified, but since German AC3 is ordered before AAC, that will appear before any other AAC tracks.   And then you have German AAC 5.1 which will be ordered before the Stereo track of the Dutch AAC track.
