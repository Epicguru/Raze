using Microsoft.Xna.Framework.Graphics;

namespace RazeUI.Providers.Implementations
{
    public class MonoGameScreenProvider : IScreenProvider
    {
        public GraphicsDevice GraphicsDevice { get; set; }

        public MonoGameScreenProvider(GraphicsDevice gd)
        {
            this.GraphicsDevice = gd;
        }

        public int GetWidth()
        {
            return GraphicsDevice?.PresentationParameters.BackBufferWidth ?? 600;
        }

        public int GetHeight()
        {
            return GraphicsDevice?.PresentationParameters.BackBufferHeight ?? 480;
        }
    }
}
