## Requirements
1. Install Git
2. Install .NET 3.5/4
3. Install WixToolkit
4. Download ffmpeg and place in c:\utils\ffmpeg\ffmpeg.exe\

## Plugins
As the main build requires the plugins to be built to include them, build the plugins first.
Build the plugins using the build-plugins.ps1 script


## Main Build
1. Deploy these ffplugin files into the /deploy/Plugins folder
2. Build FileFlows using the build.ps1 script
3. This will build to the /deploy folder