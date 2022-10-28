namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Converts a value to upper case
/// </summary>
public class UpperCaseFormatter:Formatter
{
    /// <summary>
    /// Tests if this formatter is a match for the given format
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>true if this formatter matches the format</returns>
    public override bool IsMatch(string format) => format == "!";

    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the formatted value string</returns>
    public override string Format(object value, string format)
        => (value?.ToString() ?? string.Empty).ToUpper();
}