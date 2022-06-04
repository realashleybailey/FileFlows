using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

public class DefaultScripts
{
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Checking default Scripts");

        InsertScripts();
    }

    private void InsertScripts()
    {
	    var scripts = GetDefaultScripts();
	    // we always update the default scripts
        foreach (var script in scripts)
        {
	        // set these values to now so they wont trigger a reprocess
	        DbHelper.Update(script).Wait();
        }
    }


    private Script Script_VideoResolution()
    {
	    return new Script
	    {
		    Uid = new Guid("83f5c662-8727-4e86-a09c-e3d585d0cd20"),
		    Name = "Video: Resolution",
		    Code =
			    $@"
/**
 * Determines a video's resolution and outputs accordingly
 * @author John Andrews
 * @version {Globals.Version}
 * @output Video Is 4k
 * @output Video is 1080p
 * @output Video is 720p
 * @output Video is SD
 */
function Script()
{{
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
}}
"
	    };
    }


    private Script Script_VideoDownscaleGreaterThan1080p()
    {
	    return new Script
	    {
		    Uid = new Guid("2d8c9a03-4ed3-4406-a633-38d0e2eec806"),
		    Name = "Video: Downscale greater than 1080p",
		    Code =
			    $@"
/**
 * If a video's resolution is greater than 1080p, this script will set the variable {{EncodingParameters}} to downscale the video to 1080p.
 * Used with the VideoEncode node.
 * @author John Andrews
 * @version {Globals.Version}
 * @output Video is greater than 1080p, {{EgncodingParameters}} set to downscale
 * @output Video is not greater than 1080p
 */
function Script()
{{
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
	{{
	    // down scale to 1920 and encodes using NVIDIA
		// then add a 'Video Encode' node and in that node 
		// set 
		// 'Video Codec' to 'hevc'
		// 'Video Codec Parameters' to '{{EncodingParameters}}'
		Logger.ILog(`Need to downscale from ${{video.Width}}x${{video.Height}}`);
	    Variables.EncodingParameters = '-vf scale=1920:-2:flags=lanczos -c:v hevc_nvenc -preset hq -crf 23'
		return 1;
	}}

	Logger.ILog('Do not need to downscale');
	return 2;
}}
"
	    };
    }

    private Script Script_VideoBitrateGreaterThan()
    {
	    return new Script
	    {
		    Uid = new Guid("2e192bb0-2110-49c9-b89e-af36affd88e6"),
		    Name = "Video: Bitrate greater than",
		    Code =
			    $@"
/**
 * Checks if a video's bitrate is greater than specified.
 * @author John Andrews
 * @version {Globals.Version}
 * @param {{int}} MaxBitrateKbps The maximum to check in KBps
 * @output Video's bitrate is greater than specified
 * @output Video's bitrate is not greater than specified
 */
function Script(MaxBitrateKbps)
{{
	// check if the bitrate for a video is over a certain amount
	let MAX_BITRATE = MaxBitrateKbps * 1000; 

	let vi = Variables.vi?.VideoInfo;
	if(!vi || !vi.VideoStreams || !vi.VideoStreams[0])
		return -1; // no video information found

	// get the video stream
	let bitrate = vi.VideoStreams[0].Bitrate;

	if(!bitrate)
	{{
		// video stream doesn't have bitrate information
		// need to use the overall bitrate
		let overall = vi.Bitrate;
		if(!overall)
			return 0; // couldn't get overall bitrate either

		// overall bitrate includes all audio streams, so we try and subtract those
		let calculated = overall;
		if(vi.AudioStreams?.length) // check there are audio streams
		{{
			for(let audio of vi.AudioStreams)
			{{
				if(audio.Bitrate > 0)
					calculated -= audio.Bitrate;
				else{{
					// audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
					// this is a guess, but it should get us close
					calculated -= (overall * 0.05);
				}}
			}}
		}}
		bitrate = calculated;
	}}

	// check if the bitrate is over the maximum bitrate
	if(bitrate > MAX_BITRATE)
		return 1; // it is, so call output 1
	return 2; // it isn't so call output 2
}}
"
	    };
    }


    private Script Script_7ZipCompressToZip()
    {
	    return new Script
	    {
		    Uid = new Guid("81c89bec-74c3-4a6e-a321-29ec14587292"),
		    Name = "7Zip: Compress to Zip",
		    Code =
			    $@"
/**
 * Requires a 7Zip tool to be configured and will zip files
 * @author John Andrews
 * @version {Globals.Version}
 * @param {{string}} ArchiveFile The name of the zip file to create, if empty a random name will be used
 * @param {{string}} Pattern The filename pattern to use, eg *.txt
 * @param {{bool}} SetWorkingFileToZip If the working file in the flow should be set to the newly created zip file
 * @output Zip file created
 */
function Script(ArchiveFile, Pattern, SetWorkingFileToZip)
{{
	let output = '' + ArchiveFile; // ensures ArchiveFile is a string
	if(!output || output.trim().length == 0)
		output = Flow.TempPath + '/' + Flow.NewGuid() + '.zip';
	Logger.ILog('Output: ' + output);
    let sevenZip = Flow.GetToolPath('7zip');

    let process = Flow.Execute({{
	    command: sevenZip,
	    argumentList: [
		    'a',
		    output,
		    Pattern
		]
    }});

    if(process.exitCode !== 0){{
	    Logger.ELog('Failed to zip: ' + process.exitCode);
	    return -1;
    }}

    if(SetWorkingFileToZip)
	    Flow.SetWorkingFile(output);
    return 1;
}}
"
	    };
    }

    private List<Script> GetDefaultScripts()
    {
	    var templates = new List<Script>();

	    templates.Add(Script_VideoResolution());
	    templates.Add(Script_VideoDownscaleGreaterThan1080p());
	    templates.Add(Script_VideoBitrateGreaterThan());
	    templates.Add(Script_7ZipCompressToZip());
	    return templates;
    }
}