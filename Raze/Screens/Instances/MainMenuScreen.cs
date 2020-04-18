using Microsoft.Xna.Framework.Graphics;
using RazeUI;

namespace Raze.Screens.Instances
{
    public class MainMenuScreen : GameScreen
    {

        public MainMenuScreen() : base("Main Menu")
        {
        }

        public override void DrawUI(SpriteBatch sb, LayoutUserInterface ui)
        {
            
        }

        private void OnHostClicked()
        {
            if (!Manager.IsTransitioning)
            {
                Manager.GetScreen<PlayScreen>().HostMode = true;
                Manager.ChangeScreen<PlayScreen>();
            }
        }

        private void OnConnectClicked()
        {
            if (!Manager.IsTransitioning)
                Manager.ChangeScreen<ConnectScreen>();
        }

        private void OnExitClicked()
        {
            Main.ForceExitGame();
        }
    }
}
