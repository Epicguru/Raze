using Microsoft.Xna.Framework;
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
    }
}
