using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlows.Shared.Json;

/// <summary>
/// Converts a C# TimeSpan as a long
/// </summary>
public class TimeSpanConverter: JsonConverter<TimeSpan>
{
    /// <summary>
    /// Reads the json value as a TimeSpan
    /// </summary>
    /// <param name="reader">the json reader</param>
    /// <param name="typeToConvert">the type to convert</param>
    /// <param name="options">the json serialization options</param>
    /// <returns>the TimeSpan value</returns>
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(TimeSpan));
        return new TimeSpan(reader.GetInt64());
    }

    /// <summary>
    /// Writes teh datetime value to the json writer
    /// </summary>
    /// <param name="writer">the JSON writer</param>
    /// <param name="value">the value to write</param>
    /// <param name="options">the json serialization options</param>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Ticks);
    }
}