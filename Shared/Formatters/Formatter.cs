using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.Shared.Formatters
{
    public abstract class Formatter
    {
        static Dictionary<string, Formatter> _formatters = new Dictionary<string, Formatter>()
        {
            { nameof(FileSizeFormatter), new FileSizeFormatter() }
        };

        protected abstract string Format(object value);
        
        public static string Format(string formatter, object value)
        {
            try
            {
                if (_formatters.ContainsKey(formatter ?? string.Empty))
                    return _formatters[formatter].Format(value);
                return value?.ToString() ?? string.Empty; 

            } catch (Exception ex) { return ex.Message; }
                
        }
    }
}
