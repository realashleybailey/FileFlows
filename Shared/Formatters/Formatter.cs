namespace FileFlows.Shared.Formatters;

/// <summary>
/// A formatter will format an object as a string 
/// </summary>
public abstract class Formatter
{
    /// <summary>
    /// A list of available formatter instances
    /// </summary>
    static Dictionary<string, Formatter> _formatters = new Dictionary<string, Formatter>()
    {
        { nameof(FileSizeFormatter), new FileSizeFormatter() }
    };

    /// <summary>
    /// Formats a value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <returns>the formatted value</returns>
    protected abstract string Format(object value);
    
    /// <summary>
    /// Formats a value 
    /// </summary>
    /// <param name="formatter">the formatter to use</param>
    /// <param name="value">the value to format</param>
    /// <returns>the formatted value</returns>
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