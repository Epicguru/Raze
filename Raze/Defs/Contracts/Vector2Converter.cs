using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public class Vector2Converter : CustomConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var vector2 = (Vector2)value;
            writer.WriteValue($"{vector2.X}, {vector2.Y}");
        }

        public override Vector2 Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var split = Split(reader);
            if (split == null || split.Length != 2)
                throw new Exception($"Expected 2 arguments when reading Vector2, got {split?.Length.ToString() ?? "<null>"}.");

            var x = TryConvert<float>(split[0], "X");
            var y = TryConvert<float>(split[1], "Y");

            return new Vector2(x, y);
        }
    }
}
