using GVS.Networking.Players;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.Screens.Instances
{
    public partial class PlayScreen
    {
        private Texture2D playerIcon;
        
        private void LoadUIData()
        {
            if(playerIcon == null)
            {
                playerIcon = Main.ContentManager.Load<Texture2D>("Textures/PlayerIcon");
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
