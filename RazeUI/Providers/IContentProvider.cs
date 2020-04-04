using Microsoft.Xna.Framework.Graphics;
using RazeContent;
using RazeUI.UISprites;

namespace RazeUI.Providers
{
    public interface IContentProvider
    {
        UISprite LoadSprite(string localPath);
        GameFont LoadFont(string localPath);
    }
}
