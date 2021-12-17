using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using System.Text.RegularExpressions;

namespace FileFlows.Node
{
    public class ConnectionTester
    {
        public static (bool, string) SaveConnection(string url, string tempPath, int runners, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (false, string.Empty);

            url = url.Trim().ToLower();
            if (url.StartsWith("http") == false)
                url = "http://" + url;
            if (url.EndsWith("/") == false)
                url += "/";
            var match = Regex.Match(url, "http(s)?://[^/]+/");
            if (match.Success == false)
                return (false, string.Empty);

            url = match.Value;

            try
            {
                var nodeService = new NodeService();
                var result = nodeService.Register(url, Environment.MachineName, tempPath, runners, enabled).Result;
                if (result == null)
                    return (false, "Failed to register");
                return (true, url);

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
