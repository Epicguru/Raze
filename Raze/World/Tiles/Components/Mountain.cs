using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;

namespace Raze.World.Tiles.Components
{
    public class Mountain : TileComponent
    {
        public override void Draw(SpriteBatch spr)
        {
            spr.Draw(Main.MountainTile, GetDrawPosition(), Color.White, GetDrawDepth());
        }
    }
}
