namespace PluginInfoGenerator
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using FileFlows.Shared.Models;

    public class PluginInfoConverter : JsonConverter<PluginInfo>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(PluginInfo) == typeToConvert;
        }

        public override PluginInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, PluginInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var properties = value.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name == nameof(PluginInfo.Deleted))
                    continue;
                if (prop.Name == nameof(PluginInfo.Uid))
                    continue;
                if (prop.Name == nameof(PluginInfo.DateCreated))
                    continue;
                if (prop.Name == nameof(PluginInfo.DateModified))
                    continue;
                if (prop.Name == nameof(PluginInfo.Enabled))
                    continue;

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

            writer.WriteEndObject();

        }
    }
}