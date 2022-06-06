![image](https://user-images.githubusercontent.com/958400/167319454-034444a2-f731-4453-8d16-566448ae3dce.png)


This node will search for matching files and add them as input files to FFMPEG Builder.

This can be used to add external subtitles or external audio sources etc.

![image](https://user-images.githubusercontent.com/958400/167319433-322f61df-d201-4a89-b8e5-80ed753034a3.png)


### Options
#### Pattern
A regular expression to test the filename against.  Is not cases sensitive

#### Use Source Directory
If checked the original source directory will be searched, otherwise the working directory will be used.

### Outputs
1. File found and added as input
2. No matching file found and nothing added