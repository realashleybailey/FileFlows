---
title: Video Nodes > FFMPEG Builder > Metadata Remover
permalink: /plugins/video-nodes/ffmpeg-builder/metadata-remover
name: Metadata Remover
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

{% include node.html input=1 outputs=1 icon="fas fa-remove-format" name="FFMPEG Builder: Metadata Remover" type="BuildPart" %}

Removes metadata from the FFMPEG Builder so when the file is processed the selected metadata will be removed.

Note: Only the metadata when this node is effected, if metadata is added after this node runs, that will not be effected.


### Fields

#### Video
If video tracks should have this metadata removed

#### Audio
If audio tracks should have this metadata removed

#### Subtitle
If subtitle tracks should have this metadata removed

#### RemoveImages
If any images found in the metadata should be removed

#### Remove Title
If the title should be removed from the tracks

#### Remove Language
If the language should be removed from the tracks