
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

        public static bool InitDone => Formatter != null;

        public static bool NeedsTranslating(string label) => rgxNeedsTranslating.IsMatch(label ?? "");
        public static void Init(params string[] jsonFiles)
        {
            Formatter = new MessageFormatter();

            foreach (var json in jsonFiles)
            {
                try
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
                catch (Exception) { }
            }

            Logger?.DLog("Language keys found: " + Language.Keys.Count);
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

        private static string Lookup(string[] possibleKeys, bool supressWarnings = false)
        {
            foreach (string key in possibleKeys)
            {
                if (String.IsNullOrWhiteSpace(key))
                    continue;
                if (Language.ContainsKey(key))
                    return Language[key];
            }
            if (possibleKeys[0].EndsWith("-Help") || possibleKeys[0].EndsWith("-Placeholder") || possibleKeys[0].EndsWith("-Suffix") || possibleKeys[0].EndsWith("-Prefix") || possibleKeys[0].EndsWith(".Description"))
                return "";

            if (possibleKeys[0].EndsWith(".Name") && Language.ContainsKey("Labels.Name"))
                return Language["Labels.Name"];

            string result = possibleKeys?.FirstOrDefault() ?? "";
            if(supressWarnings == false)
                Logger?.WLog("Failed to lookup key: " + result);
            result = result.Substring(result.LastIndexOf(".") + 1);

            return result;
        }

        public static string Instant(string key, object parameters = null, bool supressWarnings = false)
            => Instant(new[] { key }, parameters, supressWarnings: supressWarnings);

        public static string Instant(string[] possibleKeys, object parameters = null, bool supressWarnings = false)
        {
            try
            {
                string msg = Lookup(possibleKeys, supressWarnings: supressWarnings);
                if (msg == "")
                    return "";
                if (parameters is IDictionary<string, object> dict)
                    return Formatter.FormatMessage(msg, dict);

                return Formatter.FormatMessage(msg, parameters ?? new { });
            }
            catch (Exception ex)
            {
                if(supressWarnings == false)
                    Logger?.WLog("Failed to translating key: " + possibleKeys[0] + ", " + ex.Message);
                return possibleKeys[0];
            }
        }

        public static string TranslateIfHasTranslation(string key, string @default)
        {
            try
            {
                if (Language.ContainsKey(key) == false)
                    return @default;
                string msg = Language[key];
                return Formatter.FormatMessage(msg, new { });
            }
            catch (Exception ex)
            {
                return @default;
            }
        }
    }
}