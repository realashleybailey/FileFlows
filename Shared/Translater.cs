
namespace ViWatcher.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Jeffijoe.MessageFormat;
    using Newtonsoft.Json.Linq;

    public class Translater
    {
        private static MessageFormatter Formatter;
        private static Dictionary<string, string> Language { get; set; } = new Dictionary<string, string>();
        private static Regex rgxNeedsTranslating = new Regex(@"^([\w\d_\-]+\.)+[\w\d_\-]+$");

        public static string TranslateIfNeeded(string value)
        {
            if(string.IsNullOrEmpty(value))
                return String.Empty;
            if(NeedsTranslating(value) == false)
                return value;
            return Instant(value);
        }

        public static bool NeedsTranslating(string label) => rgxNeedsTranslating.IsMatch(label ?? "");
        public static void Init(params string[] jsonFiles)
        {
            Formatter = new MessageFormatter();

            foreach (var json in jsonFiles)
            {
                var dict = DeserializeAndFlatten(json);
                foreach (var key in dict.Keys)
                {
                    if (Language.ContainsKey(key))
                        Language[key] = dict[key];
                    else
                        Language.Add(key, dict[key]);
                }
            }

            Console.WriteLine(Language.Keys.Count);
        }
        public static Dictionary<string, string> DeserializeAndFlatten(string json)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "");
            return dict;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, string> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value.ToString());
                    break;
            }
        }
        private static string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }

        private static string Lookup(string[] possibleKeys)
        {
            foreach (string key in possibleKeys)
            {
                if (String.IsNullOrWhiteSpace(key))
                    continue;
                if (Language.ContainsKey(key))
                    return Language[key];

            }
            if(possibleKeys[0].EndsWith("-Help") || possibleKeys[0].EndsWith("-Placeholder"))
                return "";

            string result = possibleKeys?.FirstOrDefault() ?? "";
            result = result.Substring(result.LastIndexOf(".") + 1);

            return result;
        }

        public static string Instant(string key, object parameters = null) 
            => Instant(new[] { key }, parameters);

        public static string Instant(string[] possibleKeys, object parameters = null)
        {
            try
            {
                string msg = Lookup(possibleKeys);
                if(msg == "")
                    return "";
                return Formatter.FormatMessage(msg, parameters ?? new {});
            }
            catch(Exception ex)
            {
                return possibleKeys[0];
            }
        }
    }
}