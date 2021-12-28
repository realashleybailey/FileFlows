using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.ServerShared.Helpers
{
    public class FileHelper
    {

        /// <summary>
        /// Removes illegal file/path characters from a string
        /// </summary>
        /// <param name="input">the string to clean</param>
        /// <returns>the original string with all illegal characters removed</returns>
        public static string RemoveIllegalCharacters(string input)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (var c in invalid)
                input = input.Replace(c.ToString(), string.Empty);
            return input;
        }
    }
}
