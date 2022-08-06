using System.Text.Json;

namespace FileFlows.Client.Components.Inputs;

public partial class InputMetadata : Input<Dictionary<string, object>>
{
    private string FormatBitrate(object o)
    {
        if (o == null)
            return string.Empty;
        if (o is JsonElement je)
        {
            var bitrate = je.GetDouble();
            if (bitrate > 1_000_000)
                return Math.Round(bitrate / 1_000_000, 1) + " Mbps";
            else if(bitrate > 1_000)
                return Math.Round(bitrate / 1_000, 1) + " Kbps";
            return bitrate + " bps";
        }

        return o.GetType().FullName;
    }
}