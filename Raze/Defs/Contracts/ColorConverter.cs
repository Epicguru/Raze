using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public class ColorConverter : CustomConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var col = (Color)value;
            writer.WriteValue($"{col.R}, {col.G}, {col.B}, {col.A}");
        }

        public override Color Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var split = base.Split(reader);
            if (split == null || split.Length < 3 || split.Length > 4)
                throw new Exception($"Expected 3 or 4 arguments when reading Color, got {split?.Length.ToString() ?? "<null>"}.");

            var r = TryConvert<byte>(split[0], "R");
            var g = TryConvert<byte>(split[1], "G");
            var b = TryConvert<byte>(split[2], "B");
            byte a = 255;
            if(split.Length == 4)
                a = TryConvert<byte>(split[3], "A");

            return new Color(r, g, b, a);
        }
    }
}
