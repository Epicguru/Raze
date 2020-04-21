using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public class TypeConverter : JsonConverter
    {
        public static readonly TypeConverter Instance = new TypeConverter();

        private TypeConverter() { }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // There should not be an existing value, but if there is it will be overwritten.
            Debug.Assert(objectType == typeof(Type));

            string s = (string) reader.Value;

            return DefinitionLoader.FindType(s);
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }
}
