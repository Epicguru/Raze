using Microsoft.Xna.Framework.Graphics;

namespace Raze.Entities.Instances
{
    internal class DevTroop : Entity
    {
        public DevTroop()
        {
            Name = "Dev Troop";
        }

        protected override void Draw(SpriteBatch sb)
        {
            DrawSprite(sb, Main.MissingTextureSprite);
        }
    }
}
