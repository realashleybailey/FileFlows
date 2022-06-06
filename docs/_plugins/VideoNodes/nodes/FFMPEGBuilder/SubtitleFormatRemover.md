---
name: Subtitle Format Remover
layout: default
plugin: Video Nodes
sub: FFMPEG Builder
---

![image](https://user-images.githubusercontent.com/958400/164948397-ac74efa4-c496-456d-abcc-553d885fd0e1.png)

This node will update the "FFMPEG Builder" to remove subtitles that are in the desired format,  or "All" if that is checked, in the final output file.

Nothing is processed until the "FFMPEG Builder: Executor" is called.

Only subtitles that are currently known when this node is called will be removed.  If another node then adds a subtitle, that subtitle will not be removed by this node.

### Outputs
1. Subtitles were found and marked for removal
2. Subtitles were not found and not marked for removal