namespace FileFlow.Shared
{
    public static class ExtensionMethods
    {
        public static string ToJson(this object o)
        {
            if (o == null)
                return "";
            return System.Text.Json.JsonSerializer.Serialize(o);
        }

        public static string EmptyAsNull(this string str)
        {
            return str == string.Empty ? null : str;
        }
    }
}