﻿namespace FileFlows.Plugin
{
    using System.Text.RegularExpressions;

    public class VariablesHelper
    {
        /// <summary>
        /// Replaces variables in a given string
        /// </summary>
        /// <param name="input">the input string</param>
        /// <param name="variables">the variables used to replace</param>
        /// <param name="stripMissing">if missing variables shouild be removed</param>
        /// <param name="cleanSpecialCharacters">if special characters (eg directory path separator) should be replaced</param>
        /// <param name="encoder">Optional function to encode the variable values before replacing them</param>
        /// <returns>the string with the variables replaced</returns>
        public static string ReplaceVariables(string input, Dictionary<string, object> variables, bool stripMissing = false, bool cleanSpecialCharacters = false, Func<string, string> encoder = null)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            
            foreach(Match match in Regex.Matches(input, @"{([^|}]+)\|([^}]+)}"))
            {
                object value = match.Groups[1].Value;
                if (variables != null && variables.ContainsKey((string)value))
                    value = variables[(string)value];
                
                var format = match.Groups[2].Value;
                var formatter = Formatters.Formatter.GetFormatter(format);
                string strValue = formatter.Format(value, format);
                
                if(encoder != null)
                    strValue = encoder(strValue);

                input = input.Replace(match.Value, strValue);
            }
        
            if (variables?.Any() == true)
            {
                foreach (string variable in variables.Keys)
                {
                    string strValue = variables[variable]?.ToString() ?? "";
                    if (cleanSpecialCharacters && variable.Contains(".") && variable.StartsWith("file.") == false && variable.StartsWith("folder.") == false)
                    {
                        // we dont want to replace user variables they set themselves, eg they may have set "DestPath" or something in the Function node
                        // so we dont want to replace that, or any of the file/folder variables
                        // but other nodes generate variables based on metadata, and that metadata may contain a / or \ which would break a filename
                        strValue = strValue.Replace("/", "-");
                        strValue = strValue.Replace("\\", "-");
                    }
                    input = ReplaceVariable(input, variable, strValue, variables[variable], encoder);
                }
            }

            if (stripMissing)
                input = Regex.Replace(input, "{[^}]*}", "");

            return input;
        }

        private static string ReplaceVariable(string input, string variable, string value, object actualValue, Func<string, string> encoder = null)
        {
            string result = input;
            if(Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"}"))
                result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"}", value, RegexOptions.IgnoreCase);
            if (Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"!}"))
                result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"!}", value.ToUpper(), RegexOptions.IgnoreCase);
            // if (Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"[:\|][0#]+}"))
            // {
            //     var match = Regex.Match(result, @"{" + Regex.Escape(variable) + @"[:\|][0#]+}").Value;
            //     match = match.Substring(match.Replace(":", "|").LastIndexOf("|") + 1);
            //     int digits = match.Length - 1;
            //     if (actualValue is int iValue)
            //         value = iValue.ToString(new string('0', digits));
            //     else if(actualValue is double dValue)
            //         value = dValue.ToString(new string('0', digits));
            //     else if (actualValue is Int64 i64Value)
            //         value = i64Value.ToString(new string('0', digits));
            //
            //     if(encoder != null)
            //         value = encoder(value);
            //
            //     result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"[:\|][0#]+}", value, RegexOptions.IgnoreCase);
            // }

            return result;
        }

    }
}
