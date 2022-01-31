namespace FileFlows.Plugin
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
        /// <returns>the string with the variables replaced</returns>
        public static string ReplaceVariables(string input, Dictionary<string, object> variables, bool stripMissing = false, bool cleanSpecialCharacters = false)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

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
                    input = ReplaceVariable(input, variable, strValue);
                }
            }

            if (stripMissing)
                input = Regex.Replace(input, "{[^}]*}", "");

            return input;
        }

        private static string ReplaceVariable(string input, string variable, string value)
        {
            string result = input;
            if(Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"}"))
                result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"}", value, RegexOptions.IgnoreCase);
            if (Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"!}"))
                result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"!}", value.ToUpper(), RegexOptions.IgnoreCase);
            return result;
        }

    }
}
