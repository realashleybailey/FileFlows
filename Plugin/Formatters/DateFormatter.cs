using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Formats a value as a date
/// </summary>
public class DateFormatter : Formatter
{
    /// <summary>
    /// Tests if this formatter is a match for the given format
    /// </summary>
    /// <param name="format">the format to use</param>
    /// <returns>true if this formatter matches the format</returns>
    public override bool IsMatch(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;
        switch (format.ToLower())
        {
            case "date":
            case "time":
            case "datetime":
                return true;
            default:
                return Regex.IsMatch(format, @"^[dMyhHmsft\s:\-_]{2,}$");
        }
    }

    /// <summary>
    /// Formats the value
    /// </summary>
    /// <param name="value">the value to format</param>
    /// <param name="format">the format to use</param>
    /// <returns>the formatted value string</returns>
    public override string Format(object value, string format)
    {
        if (value == null)
            return string.Empty;
        
        DateTime dt = DateTime.MinValue;
        if (value is DateTime dt2)
            dt = dt2;
        else if(value is long lTicks)
            dt = new DateTime(lTicks);
        else if(value is double dTicks)
            dt = new DateTime((long)dTicks);
        else
        {
            try
            {
                dt = DateTime.Parse(value.ToString());
            }
            catch (Exception)
            {
                return value.ToString();
            }
        }

        if (dt == DateTime.MinValue)
            return value.ToString();

        if (format.ToLower() == "date")
            return dt.ToShortDateString();
        if (format == "time")
            return dt.ToShortTimeString();
        if(format == "datetime")
            return dt.ToString();
        return dt.ToString(format);
    }
}