using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public abstract class CustomConverter<T> : JsonConverter
    {
        private static readonly string[] sep = { "," };

        public sealed override bool CanRead
        {
            get
            {
                return canRead;
            }
        }
        public sealed override bool CanWrite
        {
            get
            {
                return canWrite;
            }
        }

        private bool canWrite = false;
        private bool canRead = true;
        private Type currentType;

        public void SetCanWrite(bool flag)
        {
            this.canWrite = flag;
        }

        public void SetCanRead(bool flag)
        {
            this.canRead = flag;
        }

        protected string ReadString(JsonReader reader)
        {
            if (reader.Value == null)
                return null;

            if (reader.TokenType != JsonToken.String)
                return null;

            return reader.Value as string;
        }

        protected string[] Split(JsonReader reader)
        {
            string raw = ReadString(reader);
            return raw?.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        }

        protected TC TryConvert<TC>(string txt, string name)
        {
            switch (Type.GetTypeCode(typeof(TC)))
            {
                case TypeCode.Int32:
                    if(!int.TryParse(txt, out int res))
                        throw new ArgumentException($"Could not parse '{txt}' into [{typeof(TC).Name}] {currentType.Name}.{name}");
                    return (TC)Convert.ChangeType(res, typeof(TC));

                case TypeCode.Byte:
                    if (!byte.TryParse(txt, out byte res2))
                        throw new ArgumentException($"Could not parse '{txt}' into [{typeof(TC).Name}] {currentType.Name}.{name}");
                    return (TC)Convert.ChangeType(res2, typeof(TC));

                case TypeCode.Single:
                    if (!float.TryParse(txt, out float res3))
                        throw new ArgumentException($"Could not parse '{txt}' into [{typeof(TC).Name}] {currentType.Name}.{name}");
                    return (TC)Convert.ChangeType(res3, typeof(TC));

                default:
                    throw new NotImplementedException($"Cannot convert to {typeof(TC).FullName}.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException($"There is no implementation to write to json using this {GetType().FullName}.");
        }

        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            currentType = objectType;
            var result = Read(reader, objectType, existingValue, serializer);
            currentType = null;
            return result;
        }

        public abstract T Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

        public sealed override bool CanConvert(Type objectType)
        {
            return typeof(T) == objectType;
        }
    }
}
