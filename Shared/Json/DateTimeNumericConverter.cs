using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlows.Shared.Json;

/// <summary>
/// Converts a C# DateTime to the number of milliseconds as json
/// </summary>
public class DateTimeNumericConverter: JsonConverter<DateTime>
{
    /// <summary>
    /// Reads the json value as a DateTime
    /// </summary>
    /// <param name="reader">the json reader</param>
    /// <param name="typeToConvert">the type to convert</param>
    /// <param name="options">the json serialization options</param>
    /// <returns>the DateTime value</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));
        return DateTime.Parse(reader.GetString() ?? string.Empty);
    }

    /// <summary>
    /// Writes teh datetime value to the json writer
    /// </summary>
    /// <param name="writer">the JSON writer</param>
    /// <param name="value">the value to write</param>
    /// <param name="options">the json serialization options</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToFileTimeUtc().ToString());
    }
}