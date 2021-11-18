
namespace FileFlows.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Jeffijoe.MessageFormat;
    using FileFlows.Plugin;
    using System.Text.Json;
    using System.Dynamic;

    public class Translater
    {
        private static MessageFormatter Formatter;
        private static Dictionary<string, string> Language { get; set; } = new Dictionary<string, string>();
        private static Regex rgxNeedsTranslating = new Regex(@"^([\w\d_\-]+\.)+[\w\d_\-]+$");

        public static ILogger Logger { get; set; }

        public static string TranslateIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value))
                return String.Empty;
            if (NeedsTranslating(value) == false)
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
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters = { new LanguageConverter() }
            };
            dynamic d = JsonSerializer.Deserialize<ExpandoObject>(json, options);
            FillDictionaryFromExpando(dict, d, "");
            return dict;
        }

        private static void FillDictionaryFromExpando(Dictionary<string, string> dict, ExpandoObject expando, string prefix)
        {
            var dictExpando = (IDictionary<string, object>)expando;
            foreach (string key in dictExpando.Keys)
            {
                if (dictExpando[key] is ExpandoObject eo)
                {
                    FillDictionaryFromExpando(dict, eo, Join(prefix, key));
                }
                else if (dictExpando[key] is string str)
                    dict.Add(Join(prefix, key), str);

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
            if (possibleKeys[0].EndsWith("-Help") || possibleKeys[0].EndsWith("-Placeholder") || possibleKeys[0].EndsWith(".Description"))
                return "";

            string result = possibleKeys?.FirstOrDefault() ?? "";
            Logger?.WLog("Failed to lookup key: " + result);
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
                if (msg == "")
                    return "";
                return Formatter.FormatMessage(msg, parameters ?? new { });
            }
            catch (Exception ex)
            {
                Logger?.WLog("Failed to translating key: " + possibleKeys[0] + ", " + ex.Message);
                return possibleKeys[0];
            }
        }
    }
}