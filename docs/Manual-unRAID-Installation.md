Until this is available in the unRAID Community Addons,  these are the instructions to install into unRAID.


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
