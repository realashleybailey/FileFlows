using System.Text.Json;
using System.Text.Json.Serialization;
using FileFlows.Server.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers
{
    public class DataConverter : JsonConverter<FileFlowObject>
    {
        public override FileFlowObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, FileFlowObject value, JsonSerializerOptions options)
        {
            var properties = value.GetType().GetProperties();

            writer.WriteStartObject();

            foreach (var prop in properties)
            {
                // dont write the properties that also exist on the DbObject
                if ((prop.Name == "Uid" || prop.Name == "DateModified" || prop.Name == "DateCreated" || prop.Name == "Name") == false)
                {
                    var propValue = prop.GetValue(value);
                    if (propValue == null)
                        continue; // dont write nulls
                    if (prop.PropertyType.IsPrimitive && propValue == Activator.CreateInstance(prop.PropertyType))
                        continue; // dont write defaults
                    if (propValue as bool? == false)
                        continue; // don't write default false booleans

                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
    }

    public class BoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
             => reader.GetInt32() == 1;

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value ? 1 : 0);
    }
}