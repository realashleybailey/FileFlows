using System.Text.RegularExpressions;

namespace FileFlows.Shared
{
    public static class ExtensionMethods
    {
        public static string ToJson(this object o)
        {
            if (o == null)
                return "";
            return System.Text.Json.JsonSerializer.Serialize(o);
        }

        public static string? EmptyAsNull(this string str)
        {
            return str == string.Empty ? null : str;
        }

        public static bool TryMatch(this Regex regex, string input, out Match match)
        {
            match = regex.Match(input ?? string.Empty);
            return match.Success;
        }
    }
}