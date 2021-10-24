namespace ViWatcher.Shared
{
    using Newtonsoft.Json;

    public static class ExtensionMethods
    {
        public static string ToJson(this object o)
        {
            if (o == null)
                return "";
            return JsonConvert.SerializeObject(o);
        }

        public static string EmptyAsNull(this string str)
        {
            return str == string.Empty ? null : str;
        }
    }
}