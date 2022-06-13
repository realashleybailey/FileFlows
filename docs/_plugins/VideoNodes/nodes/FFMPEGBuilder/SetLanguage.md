---
title: Video Nodes > FFMPEG Builder > Set Language
permalink: /plugins/video-nodes/ffmpeg-builder/set-language
name: Set Language
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

{% include node.html input=1 outputs=2 icon="fas fa-comment-dots" name="FFMPEG Builder: Set Language" type="BuildPart" %}


This node will look for any tracks that have no language code set on them, and if found will set the language code to the one specified.

### Outputs
1. Tracks found with no language codes, and have been updated with language codes
2. No tracks with missing language codes found