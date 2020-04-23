using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;

namespace Raze.Entities.Instances
{
    public class BasicEntity : Entity
    {
        protected BasicEntity(EntityDef def) : base(def)
        {

        }

        protected override void Draw(SpriteBatch sb)
        {
            sb.Draw(GetSprite(Direction), GetDrawPosition(), SpriteTint, GetDrawDepth());
            //DrawSprite(sb, Sprite, SpriteTint, Vector2.Zero);
        }
    }
}
