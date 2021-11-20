# FileFlows

FileFlows is a docker application that lets you configure a "Flow" that can be processed on a file.

It allows you to monitor directories, and then process files matching certain criteria for processing

It is a plugin based system written in .net and comes with some plugins out of the box.

It includes basic file processing like copy, move, delete, rename, file size, and javascript functions.

The Video plugin lets you process video files to ensure they are in the format you desire and also things like removing black bars from videos.

![image](https://user-images.githubusercontent.com/958400/142393794-38b58e23-2b05-45b1-8eb1-2f4ad6574422.png)


![image](https://user-images.githubusercontent.com/958400/142393881-9a1ebac1-2b8d-4157-9371-7a9a10dee877.png)


# Unraid Installation
This application is designed (for now, later alternative builds will be published) to be run as a docker application and primarily for unRAID.

1. Select "Apps" to go to the "Community Applications" of unRAID (if not installed refer to the unRAID forum/site for installation instructions)
2. Search "FileFlows".  This won't be found in the Community Applications and click "Click Here To Get More Results From DockerHub"
   1. If that option does not appear go to Unraid \ Settings \ Community Applications.   And then set "Enable developer mode" to "Yes".
3. Select "Install" 
   ![image](https://user-images.githubusercontent.com/958400/142372817-4582c5bb-6108-42d7-8ada-f0015652c429.png)
4. Ensure "Advanced View" is on
   ![image](https://user-images.githubusercontent.com/958400/142372935-124e75b3-3e4b-4c27-827b-1c104aaefc17.png)
5. For icon URL: https://raw.githubusercontent.com/revenz/FileFlows/master/icon.png?raw=true
6. WebUI: http://[IP]:[PORT:8585]
7. Extra Parameters: --runtime=nvidia
   1. Only if using an NVIDIA GPU
8. Add Variable
   1. Name: NVIDIA_VISIBLE_DEVICES
   2. Key: NVIDIA_VISIBLE_DEVICES
   3. Value: [GPU UID FROM NVIDIA DRIVER SETTINGS IN UNRAID]
9. Add Port
   1.  Name: 5000
   2.  Container Port: 5000
   3.  Host Port: 8585
       1.  Unless you used a different port in the WebUI variable
10. Add Path
    1.  Name: /temp
    2.  Container Path: /temp
    3.  Host Path: [path on unraid, preferrable in the cache drive or a fast drive]
11. Add Path
    1.  Name:  Logs
    2.  Container Path: /app/Logs
    3.  Host Path: /mnt/user/appdata/fileflows/logs
12. Add Path
    1.  Name: Media
    2.  Container Path: /media
    3.  Host Path: [media path in unraid]
    4.  Note: This path is optional and can be whatever you like or as many as you like.  This is mapping the unraid paths so the docker can access them.


# Setup
Once the installed you need to configure FileFlows.
1. First you need to configure a Flow to be used in a library.  
   1. A Flow needs one input node (Input File or Video File or another Input Node)
   2. A node can have 0 or more outputs.
      1. Each node will describe what the outputs do when editing them (double click on the node to open it)
      2. Many have 2 outputs, 1st usually means the node condition was true the 2nd the node condition was false.
      3. A Flow will be excecuted until the path can not continue or there was an error.
      ![image](https://user-images.githubusercontent.com/958400/142374120-ce91c4ed-df63-43d2-b509-15755b00c78e.png)
      4. This flow will 
         1. Take a video file
         2. Check if that video file has black bars
         3. Process the file to H265 with AC3 audio
            1. It will use the output from detect black bars to remove them aswell if found
         4. It will move the file
         5. It will then delete the original folder if empty.
      5. This flow is designed to monitor a "Downloads" folder" and will move any video files into a "Converted" folder once done, which will be picked up another docker afterwards. e.g.
         1. Sonarr finds files.  
         2. Sabnzbd downloads files to "completed" folder"
         3. FileFlows processes files moves to "converted"
         4. Sonarr watches the "converted" folder for the resulting downloaded files and then continues.
2. Add a library for files to be monitored, and select which Flow this library should use
3. Under settings enable the "Library Scanner" and "Flow Executor"

## Flow Executor
This is the worker responsible for processing Library Files.  When enabled it will work through any Library Files awaiting to be proccessed.  

## Library Scanner
This is responsible for monitoring the libraries and will scan for new files.   
