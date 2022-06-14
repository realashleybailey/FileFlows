---
title: Versions
name: Versions
permalink: /versions
layout: default
order: 800
---

## Version 0.8.0

#### New
- External Database Support (MySQL / Maria)
  - Library Process Order: As Found (default), Random, Newest, Smallest, Largest
  - Enhanced logging, can view and search Server logs and Node logs all from the web console
- "Pause" / "Resume" button now prompts for duration to pause for
- Logging now logs to rolling log files which are kept for a configured amount of days
- Links now have "noreferrer" on them
- WAL mode is now used for SQLite database connections
- Node: [FFMPEG Builder: Custom Parameters](https://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/custom-parameters)
- Node: [FFMPEG Builder: Video Tag](http://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/video-tag)
- Node: [FFMPEG Builder: Set Language](http://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/set-language)
  - This node was the "FFMPEG Build: Audio Set Language" but has been renamed and extended to support subtitles
- Node: [FFMPEG Builder: Track Reorder](http://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/track-reorder)
  - This node was the "FFMPEG Build: Audio Track Reorder" but has been renamed and extended to support subtitles
- Node now looks for additional Environmental variables for settings: NodeMappings, NodeRunnerCount, NodeEnabled

#### Fixed
- Node: [FFMPEG Builder: Video 10 Bit](http://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/video-10-bit) and [FFMPEG Builder: Video Encode](http://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/video-encode) updated 10 Bit parameters to use p010le

#### Patreon Only Features
- External database support
- Enhanced Logging
- Auto Updates
- Up to 10 Processing Nodes


---


## Version 0.7.1

#### New
- Added "Pause" / "Resume" button.  I will extend this with a "Pause for [x] minutes" in a later version.
- Node: [Can Use Hardware Encoding](https://docs.fileflows.com/plugins/video-nodes/logical-nodes/can-use-hardware-encoding)

#### Fixed
- The logging "Log Queue Messages" switch was inversed so when it was on, it was actually off and vice versa.
- Issue with parsing comments in scripts if comment contained a "/" character
- Script output labels not shown in the Flow
- Editor would show the "Pending Changes" prompt if a code editor was previously opened.  Eg edit a script, then go to Files and view a file.  Closing that side window would cause the prompt to appear
- Floats were not allowed in a number field when should be.  This caused the "Video: Has Stream" to fail if trying to set channels to 5.1
- Docker Nodes now have the correct mapping of FFMPEG when registered

#### Updates
- Documentation now points to the new documentation site https://docs.fileflows.com


---


## Version 0.7.0

#### New
- [Scripts](https://docs.fileflows.com/scripts) new feature to allow reuse of Javascript function and easily share with other community members
- Library Files are now called "Files" to avoid confusion
- Library Exclusion Filter
  - This allows you to exclude files easily from a library  
- "Log Queue Messages" setting to reduce excess logging.  Turn this on if you're trying to debug why a file is not being detected.

#### Improvements
- Updated to include FFMPEG 5.0
- Files Paging
- Flows now have a "Duplicate" button
- Internal Processing Node now has constant UID, this avoids the potential issue of it being added more than once
- Plugins all now have constant UID, this avoids the potential issue of them being added more than once
- Fixed memory issue where the .net garbage collector was not running when it should
- Numerous other improvements to reduce memory footprint

#### Fixed
- Node "Video Has Stream" now works
- Node "FFMPEG Builder: Video Encode" now uses preset "slower" for QSV instead of p6


---


## Version: 0.6.2

#### New
- Flow Runner now sends a "hello" message to the server when processing, to say it's alive, every 5 seconds. 
- Worker that will automatically cancel any runners that have not sent an update in 60 seconds.

#### Improvements
-Moved "Dequeued" messages to debug log

#### Fixed
- Issue with log file pruner being too aggressive and deleting any log
- Issue when trying to delete a flow connection and the previously selected node was deleted instead
- Issue with "Copy File" node when Server was a Linux server and Node was a Windows Node
