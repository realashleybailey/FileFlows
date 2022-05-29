using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class Upgrade0_7_0
{
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 0.7.0 upgrade script");

        InsertScripts();
    }

    private void InsertScripts()
    {
	    var scripts = GetDefaultScripts();
        var existing = DbHelper.Select<Script>().Result.ToDictionary(x => x.Name, x => x);
        foreach (var script in scripts)
        {
	        if (existing.ContainsKey(script.Name))
		        continue;
        
            // set these values to now so they wont trigger a reprocess
            DbHelper.Update(script).Wait();
        }
    }


    private Script Script_VideoResolution()
    {
	    return new Script
	    {
		    Name = "Video: Resolution",
		    Author = "John Andrews",
		    Description = "Determines a video's resolution and outputs accordingly",
		    Outputs = new ScriptOutput[]
		    {
			    new() { Output = 1, Description = "Video is 4K" },
			    new() { Output = 2, Description = "Video is 1080p" },
			    new() { Output = 3, Description = "Video is 720p" },
			    new() { Output = 4, Description = "Video is SD" },
		    },
		    Code =
			    @"
// get the first video stream, likely the only one
let video = Variables.vi?.VideoInfo?.VideoStreams[0];
if(!video)
    return -1; // no video streams detected
if(video.Width > 3700)
    return 1; // 4k 
if(video.Width > 1800)
    return 2; // 1080p
if(video.Width > 1200)
    return 3; // 720p
return 4; // SD
"
	    };
    }


    private Script Script_VideoDownscaleGreaterThan1080p()
    {
	    return new Script
	    {
		    Name = "Video: Downscale greater than 1080P",
		    Author = "John Andrews",
		    Description =
			    "If a video's resolution is greater than 1080p, this script will set the variable {EncodingParameters} to downscale the video to 1080p.\nUsed with the VideoEncode node.",
		    Outputs = new ScriptOutput[]
		    {
			    new()
			    {
				    Output = 1, Description = "Video is greater than 1080p, {EncodingParameters} set to downscale"
			    },
			    new() { Output = 2, Description = "Video is not greater than 1080p" },
		    },
		    Code =
			    @"
// this template downscales a video with a width larger than 1920 down to 1920
// it is suppose to be used before a 'Video Encode' node and can create a variable
// to use in that node
// It uses NVIDIA hardware encoding to encode to HEVC/H265
// output 1 = needs to downscale
// output 2 = does not need to downscale

// get the first video stream, likely the only one
let video = Variables.vi?.VideoInfo?.VideoStreams[0];
if (!video)
    return -1; // no video streams detected

if (video.Width > 1920)
{
    // down scale to 1920 and encodes using NVIDIA
	// then add a 'Video Encode' node and in that node 
	// set 
	// 'Video Codec' to 'hevc'
	// 'Video Codec Parameters' to '{EncodingParameters}'
	Logger.ILog(`Need to downscale from ${video.Width}x${video.Height}`);
    Variables.EncodingParameters = '-vf scale=1920:-2:flags=lanczos -c:v hevc_nvenc -preset hq -crf 23'
	return 1;
}

Logger.ILog('Do not need to downscale');
return 2;
"
	    };
    }

    private Script Script_VideoBitrateGreaterThan()
    {
	    return new Script
	    {
		    Name = "Video: Bitrate greater than",
		    Author = "John Andrews",
		    Description =
			    "Checks if a video's bitrate is greater than specified.",
		    Outputs = new ScriptOutput[]
		    {
			    new()
			    {
				    Output = 1, Description = "Video's bitrate is greater than specified"
			    },
			    new() { Output = 2, Description = "Video's bitrate is not greater than specified" },
		    },
		    Code =
			    @"
// check if the bitrate for a video is over a certain amount
let MAX_BITRATE = 3_000_000; // bitrate is 3,000 KBps

let vi = Variables.vi?.VideoInfo;
if(!vi || !vi.VideoStreams || !vi.VideoStreams[0])
	return -1; // no video information found

// get the video stream
let bitrate = vi.VideoStreams[0].Bitrate;

if(!bitrate)
{
	// video stream doesn't have bitrate information
	// need to use the overall bitrate
	let overall = vi.Bitrate;
	if(!overall)
		return 0; // couldn't get overall bitrate either

	// overall bitrate includes all audio streams, so we try and subtract those
	let calculated = overall;
	if(vi.AudioStreams?.length) // check there are audio streams
	{
		for(let audio of vi.AudioStreams)
		{
			if(audio.Bitrate > 0)
				calculated -= audio.Bitrate;
			else{
				// audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
				// this is a guess, but it should get us close
				calculated -= (overall * 0.05);
			}
		}
	}
	bitrate = calculated;
}

// check if the bitrate is over the maximum bitrate
if(bitrate > MAX_BITRATE)
	return 1; // it is, so call output 1
return 2; // it isn't so call output 2
"
	    };
    }


    private List<Script> GetDefaultScripts()
    {
	    var templates = new List<Script>();

	    templates.Add(Script_VideoResolution());
	    templates.Add(Script_VideoDownscaleGreaterThan1080p());
	    templates.Add(Script_VideoBitrateGreaterThan());
	    return templates;
    }
}