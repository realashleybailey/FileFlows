namespace FileFlows.Shared.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using FileFlows.Shared.Validators;

    public class ValidatorConverter : JsonConverter<Validators.Validator>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Validators.Validator) == typeToConvert;
        }
        public override Validator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                if (jsonDocument.RootElement.TryGetProperty("type", out JsonElement typeValue))
                {
                    string typeName = typeValue.GetString();
                    var vts = ValidatorTypes;
                    if (vts.ContainsKey(typeName) == false)
                        return new DefaultValidator();
                    var type = vts[typeName];
                    return (Validator)jsonDocument.Deserialize(type);
                }
            }
            return new DefaultValidator();
        }

        private Dictionary<string, Type> _ValidatorTypes;

        private Dictionary<string, Type> ValidatorTypes
        {
            get
            {
                if (_ValidatorTypes == null)
                {
                    _ValidatorTypes = typeof(Validator).Assembly.GetTypes()
                                    .Where(x => x.IsAbstract == false && typeof(Validator).IsAssignableFrom(x))
                                    .ToDictionary(x => x.Name, x => x);
                }
                return _ValidatorTypes;
            }
        }

        public override void Write(Utf8JsonWriter writer, Validator value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}