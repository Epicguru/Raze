using Microsoft.Xna.Framework.Graphics;
using RazeContent;
using RazeUI.UISprites;

namespace RazeUI.Providers.Implementations
{
    public class RazeContentProvider : IContentProvider
    {
        public RazeContentManager ContentManager { get; set; }

        public RazeContentProvider(RazeContentManager manager)
        {
            ContentManager = manager;
        }

        public UISprite LoadSprite(string localPath)
        {
            return new UISprite(ContentManager.Load<Texture2D>(localPath));
        }

        public GameFont LoadFont(string localPath)
        {
            return ContentManager.Load<GameFont>(localPath);
        }
    }
}
