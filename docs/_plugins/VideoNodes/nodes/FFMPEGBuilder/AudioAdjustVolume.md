---
title: Video Nodes > FFMPEG Builder > Audio Adjust Volume
name: Audio Adjust Volume
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

![image](https://user-images.githubusercontent.com/958400/164949355-a3cc967b-5d3f-4fba-a9c1-8538bcc13389.png)

This node will update "FFMPEG Builder" and adjust the volume by percentage of all currently known audio tracks that match the conditions.

### Volume Percent
The percent of the original volume the new volume should be.  If this is set to 100, then nothing will happen.  If set to 0 then the audio will become muted.  200 the audio will be twice as loud etc.


### All Audio Tracks
When this is checked all audio tracks will have their volume adjusted and the pattern will not be used.

### Pattern
This is a regular expression that will be run against the title or language code of the audio track.

The pattern is NOT case-sensitive.

### Not Matching
This will inverse the match