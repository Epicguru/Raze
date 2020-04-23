using Microsoft.Xna.Framework;
using Raze.Defs;
using Raze.Sprites;

namespace Raze.Entities
{
    public class EntityDef : Def
    {
        public Sprite[] Sprites;
        public Color SpriteTint;
        public CardinalDirection SpawnDirection;
    }
}
