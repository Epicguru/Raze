using Microsoft.Xna.Framework.Graphics;

namespace Raze.World.Tiles
{
    public class StoneTile : Tile
    {
        public StoneTile()
        {
            Name = "Stone";
        }

        public override void Draw(SpriteBatch spr)
        {
            // Check tile above for air...
            Tile above = Map.GetTile(Position.X, Position.Y, Position.Z + 1);
            BaseSprite = (above == null) ? Main.StoneTopTile : Main.StoneTile;

            base.Draw(spr);
        }
    }
}
