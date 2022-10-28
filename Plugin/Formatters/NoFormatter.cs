namespace FileFlows.Plugin.Formatters;

/// <summary>
/// No formatter, just returns the value, this avoids a null formatter
/// </summary>
public class NoFormatter: Formatter
{
    /// <summary>
    /// always returns false 
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>returns false</returns>
    public override bool IsMatch(string format) => false;


    /// <summary>
    /// Returns the value cast to a string or an empty string if the value is null
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the value string</returns>
    public override string Format(object value, string format)
        => value == null ? string.Empty : value.ToString();
}