using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RazeUI;
using System;

namespace Raze.Screens.Instances
{
    public class MainMenuScreen : GameScreen
    {
        public MainMenuScreen() : base("Main Menu")
        {
        }

        public override void DrawUI(SpriteBatch sb, LayoutUserInterface ui)
        {
            int sw = Screen.Width;
            int sh = Screen.Height;

            ui.FontSize = 128;
            var size = ui.MeasureString("Raze");
            ui.IMGUI.Label(new Point((sw - size.X) / 2, 40), "Raze", Color.Black, out Rectangle titleBounds);
            ui.FontSize = ui.DefaultFontSize;

            int w = 400;
            int h = 500;
            ui.PanelContext(new Rectangle((sw - w) / 2, Math.Max((sh - h) / 2, titleBounds.Bottom + 10), w, h));
            if (ui.Button("Play"))
            {
                OnHostClicked();
            }
            if (ui.Button("Join Multiplayer"))
            {
                OnConnectClicked();
            }

            var panel = ui.GetContextInnerBounds();
            int height = ui.IMGUI.ApplyScale(50);
            if (ui.IMGUI.Button(new Rectangle(panel.X, panel.Bottom - height, panel.Width, height), "Quit"))
            {
                OnExitClicked();
            }
            ui.EndContext();
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
