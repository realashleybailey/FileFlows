using System.Net;
using System.Text;

namespace FileFlows.Shared.Helpers;

public class LogToHtml
{
    public static string Convert(string log)
    {
        StringBuilder colorized = new StringBuilder();

        foreach (var line in log.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                colorized.Append("<div class=\"line\">");
                colorized.Append(ColorizeSection(line));
                colorized.Append("</div>" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                colorized.Append(HtmlEncode(ex.Message));
            }
        }
        string result = colorized.ToString();
        return result;
    }

    static Regex regTime = new Regex(@"[\d]{2}:[\d]{2}:[\d]{2}\.[\d]+");
    static Regex regKeyValue = new Regex(@"^([^\s\[][^:]+[^\s]):([\s].*?$)?$");
    static Regex regDatedLine = new Regex(@"^([\d]{4}-[\d]{2}-[\d]{2} [\d]{2}:[\d]{2}:[\d]{2}\.[\d]{4}) - (INFO|WARN|DBUG|ERRR) -> (.*?$)");
    static Regex regHttpMethods = new Regex(@"\[(GET|POST|PUT|DELETE)\]");
    static Regex regUrl = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?");
    static Regex regWindowsNetwork = new Regex(@"(\\\\[a-zA-Z0-9-]+\\[a-zA-Z0-9`~!@#$%^&(){}'._-]+([ ]+[a-zA-Z0-9`~!@#$%^&(){}'._-]+)*)(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)*\\?");
    static Regex regWindowsFilename = new Regex(@"([a-zA-Z]:)(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)+\\?");
    static Regex regQuotes= new Regex(@"(?<=('))[^'<>]+(?=('))");

    private static string ColorizeSection(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
            return section ?? string.Empty;
        if (section.StartsWith("===="))
        {
            return "<span class=\"heading\">" + HtmlEncode(section) + "</span>";
        }
        else if (section.StartsWith("=== ") && section.EndsWith(" ==="))
        {
            return "<span class=\"heading\">=== <span class=\"inner\">" + HtmlEncode(section[4..^4]) + "</span> ===</span>";
        }
        else if (regDatedLine.TryMatch(section, out Match dlMatch))
        {
            string date = dlMatch.Groups[1].Value;
            string type = dlMatch.Groups[2].Value;
            string content = dlMatch.Groups[3].Value;
            return "<span class=\"line-prefix\">" +
                   "<span class=\"date\">" + HtmlEncode(date) + "</span> - " +
                   "<span class=\"logtype logtype-" + type + "\">" + HtmlEncode(type) + "</span> <span class=\"arrow\">-></span>" +
                   "</span> " +
                   ColorizeSection(content);
        }

        if (regKeyValue.TryMatch(section, out Match kvMatch))
        {
            string key = kvMatch.Groups[1].Value;
            string value = kvMatch.Groups[2].Value;
            section = "<span class=\"key\">" + ColorizeSection(key) + ":</span>" +
                      "<span class=\"value\">" + ColorizeSection(value) + "</span>";
            return section;
        }
        else
        {
            section = HtmlEncode(section);
        }

        section = regHttpMethods.Replace(section, "<span class=\"http-method\">[$1]</span>");
        section = regUrl.Replace(section, "<span class=\"url\">$0</span>");
        section = regWindowsFilename.Replace(section, "<span class=\"file\">$0</span>");
        section = regTime.Replace(section, "<span class=\"time\">$0</span>");
        section = regQuotes.Replace(section.Replace("&#39;", "'"), "<span class=\"quote\">$0</span>").Replace("'", "&#39;");

        return section;
    }

    private static string HtmlEncode(string input)
    {
        input = WebUtility.HtmlEncode(input);
        input = input.Replace("&quot;", "\""); // dont need to encode this, make matching regexes harder
        return input;

    }
}
