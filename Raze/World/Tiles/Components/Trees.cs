using GVS.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.World.Tiles.Components
{
    public class Trees : TileComponent
    {
        public override void Draw(SpriteBatch spr)
        {
            spr.Draw(Main.TreeTile, GetDrawPosition(), Color.White, GetDrawDepth());
        }
    }
}
