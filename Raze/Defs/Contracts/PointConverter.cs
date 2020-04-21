using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;

namespace Raze.Defs.Contracts
{
    public class PointConverter : CustomConverter<Point>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = (Point)value;
            writer.WriteValue($"{obj.X}, {obj.Y}");
        }

        public override Point Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var split = Split(reader);
            if (split == null || split.Length != 2)
                throw new Exception($"Expected 2 arguments when reading Point, got {split?.Length.ToString() ?? "<null>"}.");

            var x = TryConvert<int>(split[0], "X");
            var y = TryConvert<int>(split[1], "Y");

            return new Point(x, y);
        }
    }
}
