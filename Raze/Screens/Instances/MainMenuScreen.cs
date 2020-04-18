namespace Raze.Screens.Instances
{
    public class MainMenuScreen : GameScreen
    {

        public MainMenuScreen() : base("Main Menu")
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
