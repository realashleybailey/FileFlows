using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FileFlows.Server.Helpers
{
    public class TemplateHelper
    {
        public static string ReplaceWindowsPathIfWindows(string json)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                return json;
            string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            json = Regex.Replace(json, "/media/([^/\"]+)/([^\"]*)", System.Web.HttpUtility.JavaScriptStringEncode(userDir + "\\$1\\$2"));
            json = Regex.Replace(json, "/media/([^\"]*)", System.Web.HttpUtility.JavaScriptStringEncode(userDir + "\\$1"));
            json = json.Replace("\"/media\"", "\"" + System.Web.HttpUtility.JavaScriptStringEncode(userDir) + "\"");
            return json;
        }
    }
}
