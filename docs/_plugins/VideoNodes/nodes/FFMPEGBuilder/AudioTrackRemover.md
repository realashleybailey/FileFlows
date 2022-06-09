---
title: Video Nodes > FFMPEG Builder > Audio Track Remover
permalink: /plugins/video-nodes/ffmpeg-builder/audio-track-remover
name: Audio Track Remover
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

![image](https://user-images.githubusercontent.com/958400/164949012-1520e929-ff4b-4002-847a-e57cdbc3b04f.png)

This node will update "FFMPEG Builder" to remove all the matching audio tracks from the output file.  

It will only mark the current tracks in the "FFMPEG Builder" for removal, so any tracks added later will NOT be affected.

### Remove All
When this is checked all current audio tracks will be removed from the output file.  This is useful if you want to have a file with only specific tracks.

### Pattern
This is a regular expression that will be run against the title or language code of the audio track.

The pattern is NOT case-sensitive.

### Use Language Code
When this is checked it will use the language code of the track, else it will use the title of the track.


### Remove Non-English Tracks Example
![image](https://user-images.githubusercontent.com/958400/164949105-c434f247-902b-44e3-ab2f-acdf9e2a8af5.png)
