using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;

namespace Raze.World.Tiles.Components
{
    public class BasicTileComponent : TileComponent
    {
        public BasicTileComponent(TileCompDef def) : base(def)
        {

        }

        public override void Update()
        {
            
        }

        public override void Draw(SpriteBatch spr)
        {
            spr.Draw(Sprite, GetDrawPosition(), SpriteTint, GetDrawDepth());
        }
    }
}
