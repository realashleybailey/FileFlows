![image](https://user-images.githubusercontent.com/958400/164886367-225f9d0d-5f6c-4df3-b063-eabf57b50057.png)



This node will first check to see if the working file is HDR.

If it is not HDR, output 2 will be called and the node will finish.

If it is HDR, an HDR to SDR filter will be applied to the first video stream of the file, and output 1 will be called.

Nothing will be processed until the "FFMPEG Builder: Executor" is called.