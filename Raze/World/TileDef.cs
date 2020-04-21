using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Raze.Defs;
using Raze.Sprites;
using System;

namespace Raze.World
{
    public class TileDef : Def
    {
        private TileDef()
        {
        }

        public Type TileClass;

        public Color BaseTint;
        public Sprite Sprite;

        [JsonIgnore]
        public ushort TileID { get; internal set; }
    }
}
