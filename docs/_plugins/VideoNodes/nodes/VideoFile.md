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