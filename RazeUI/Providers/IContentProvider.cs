using Microsoft.Xna.Framework.Graphics;
using RazeContent;

namespace RazeUI.Providers
{
    public interface IContentProvider
    {
        Texture2D LoadTexture(string localPath);
        GameFont LoadFont(string localPath);
    }
}
