using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public class Vector3Converter : CustomConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = (Vector3)value;
            writer.WriteValue($"{obj.X}, {obj.Y}, {obj.Z}");
        }

        public override Vector3 Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var split = Split(reader);
            if (split == null || split.Length != 3)
                throw new Exception($"Expected 3 arguments when reading Vector3, got {split?.Length.ToString() ?? "<null>"}.");

            var x = TryConvert<float>(split[0], "X");
            var y = TryConvert<float>(split[1], "Y");
            var z = TryConvert<float>(split[1], "Y");

            return new Vector3(x, y, z);
        }
    }
}
