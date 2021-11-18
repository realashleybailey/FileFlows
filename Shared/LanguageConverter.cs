namespace FileFlows.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    public class LanguageConverter : JsonConverter<object>
    {
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value.GetType() == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.StartArray:
                    {
                        var list = new List<object>();
                        while (reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                default:
                                    list.Add(Read(ref reader, typeof(object), options));
                                    break;
                                case JsonTokenType.EndArray:
                                    return list;
                            }
                        }
                        throw new JsonException();
                    }
                case JsonTokenType.StartObject:
                    var dict = CreateDictionary();
                    while (reader.Read())
                    {
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.EndObject:
                                return dict;
                            case JsonTokenType.PropertyName:
                                var key = reader.GetString();
                                reader.Read();
                                dict.Add(key, Read(ref reader, typeof(object), options));
                                break;
                            default:
                                throw new JsonException();
                        }
                    }
                    throw new JsonException();
                default:
                    throw new JsonException(string.Format("Unknown token {0}", reader.TokenType));
            }
        }

        protected virtual IDictionary<string, object> CreateDictionary() => new ExpandoObject();
    }
}