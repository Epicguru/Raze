using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;

namespace Raze.Screens.Instances
{
    public class SplashScreen : GameScreen
    {
        private float timer;

        public SplashScreen() : base("Splash Screen")
        {
        }

        public override void UponShow()
        {
            timer = 0f;
        }

        public override void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer > 1f)
            {
                timer = -100; // Just to make sure it doesn't trigger twice.
                Manager.ChangeScreen<MainMenuScreen>();
            }
        }

        public override void DrawUI(SpriteBatch sb)
        {
            if(Main.LoadingIconSprite != null)
            {
                sb.Draw(Main.MissingTextureSprite, new Vector2(Screen.Width * 0.5f, Screen.Height * 0.5f), Color.White, 0f);
            }
        }
    }
}
