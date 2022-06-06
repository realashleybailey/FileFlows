This node will scan the video for black bars and set the parameter "VideoCrop" if detected.

Outputs
* Output 1: Black bars detected
* Output 2: No black bars detected

Note: If passed to another Video processing node further down the flow eg "Video H265 AC3" or "Video Encode" and black bars were detected, this will force the video to be reencoded (even if already in the desired format) and have the black bars removed.