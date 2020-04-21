using Newtonsoft.Json;
using Raze.World;
using System;

namespace Raze.Defs.Contracts
{
    public class Point3DConverter : CustomConverter<Point3D>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = (Point3D)value;
            writer.WriteValue($"{obj.X}, {obj.Y}, {obj.Z}");
        }

        public override Point3D Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var split = Split(reader);
            if (split == null || split.Length != 3)
                throw new Exception($"Expected 3 arguments when reading Point3D, got {split?.Length.ToString() ?? "<null>"}.");

            var x = TryConvert<int>(split[0], "X");
            var y = TryConvert<int>(split[1], "Y");
            var z = TryConvert<int>(split[2], "Z");

            return new Point3D(x, y, z);
        }
    }
}