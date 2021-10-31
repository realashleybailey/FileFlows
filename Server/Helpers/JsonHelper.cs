using System.Buffers;
using System.Text;
using System.Text.Json;

namespace FileFlow.Server.Helpers
{
    public class JsonHelper
    {
        public static string SimpleObjectMerge(string originalJson, string newContent)
        {
            var outputBuffer = new ArrayBufferWriter<byte>();

            using (JsonDocument jDoc1 = JsonDocument.Parse(originalJson))
            using (JsonDocument jDoc2 = JsonDocument.Parse(newContent))
            using (var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = true }))
            {
                JsonElement root1 = jDoc1.RootElement;
                JsonElement root2 = jDoc2.RootElement;

                jsonWriter.WriteStartObject();

                // Write all the properties of the first document that don't conflict with the second
                foreach (JsonProperty property in root1.EnumerateObject())
                {
                    if (!root2.TryGetProperty(property.Name, out _))
                    {
                        property.WriteTo(jsonWriter);
                    }
                }

                // Write all the properties of the second document (including those that are duplicates which were skipped earlier)
                // The property values of the second document completely override the values of the first
                foreach (JsonProperty property in root2.EnumerateObject())
                {
                    property.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndObject();
            }

            return Encoding.UTF8.GetString(outputBuffer.WrittenSpan);
        }
    }
}