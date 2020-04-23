using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Raze.Networking.Players;
using RazeUI;

namespace Raze.Screens.Instances
{
    public partial class PlayScreen
    {
        private Texture2D playerIcon;
        private Texture2D compass;

        private void LoadUIData()
        {
            if(playerIcon == null)
            {
                playerIcon = Main.ContentManager.Load<Texture2D>("Textures/PlayerIcon");
                compass = Main.ContentManager.Load<Texture2D>("Textures/Compass");
            }
        }

        private void DrawInternalUI(SpriteBatch sb, LayoutUserInterface ui)
        {
            if (Input.IsKeyDown(Keys.Tab))
            {
                sb.Draw(compass, new Vector2(30, 30), Color.White);
                //sb.Draw(Main.SpriteAtlas.Texture, Vector2.Zero, Color.White);
            }
        }

        private void AddPlayerItem(Player p)
        {
            // URGTODO re-implement (add to 'scoreboard')
        }

        private void RemovePlayerItem(Player p)
        {
            // URGTODO re-implement (remove from 'scoreboard')
        }
    }
}
