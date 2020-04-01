
using GVS.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.World.Tiles.Components
{
    public class House : TileComponent
    {
        public override void Draw(SpriteBatch spr)
        {
            spr.Draw(Main.HouseTile, GetDrawPosition(), Color.White, GetDrawDepth());
        }
    }
}
