using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlows.Shared.Json;

/// <summary>
/// Converts a C# DateTime to the number of milliseconds as json
/// </summary>
public class DateTimeNumericConverter: JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));
        return DateTime.Parse(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToFileTimeUtc().ToString());
    }
}