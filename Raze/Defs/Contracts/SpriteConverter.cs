using Newtonsoft.Json;
using Raze.Sprites;
using System;

namespace Raze.Defs.Contracts
{
    public class SpriteConverter : CustomConverter<Sprite>
    {
        public SpriteConverter()
        {
            SetCanWrite(false);
        }

        public override Sprite Read(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string path = base.ReadString(reader);

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Expected a path to a Sprite, got blank or null string.");

            return Main.SpriteAtlas.Add(path);
        }
    }
}
