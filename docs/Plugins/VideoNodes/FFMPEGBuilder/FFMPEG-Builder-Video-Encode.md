![image](https://user-images.githubusercontent.com/958400/169502803-72777454-1f00-47ee-a0df-890eeb37079f.png)

This node will ALWAYS encode a video to the quality level and codec specific.

## Options
### Codec
The codec used to encode the video.

### Hardware Encoding
When checked, will test to see if hardware encoders are found on the Processing Node, and if found will use hardware encoding, otherwise will fallback to CPU encoding.
Hardware encoder order:
* NVIDIA
* QSV (Intel)
* CPU (fallback)

### Quality
A logarithmic quality scale, so small changes in the this slider cause large changes in file size/quality.
The lower the number the higher the quality.

#### H.264 Quality
Default: 23
Range: 0 - 51
0: Lossless
51: Maximum compression, worst quality
17 - 28: Range that should be used.   17-18 should provide produce visually lossless video
NVIDIA: -rc constop -qp {QualityValue} -preset p6 -spatial-aq 1
QSV: -qp {QualityValue} -preset p6
CPU: -preset p6 -crf {QualityValue}

#### H.265 Quality
Default: 28
Range: 0 - 51
0: Lossless
51: Maximum compression, worst quality
23 - 32: Range that should be used. 
NVIDIA: -rc constop -qp {QualityValue} -preset p6 -spatial-aq 1
QSV: -qp {QualityValue} -preset p6
CPU: -preset p6 -crf {QualityValue}