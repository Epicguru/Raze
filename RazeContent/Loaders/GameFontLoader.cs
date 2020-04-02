using SpriteFontPlus;
using System.IO;

namespace RazeContent.Loaders
{
    public class GameFontLoader : ContentLoader
    {
        public GameFontLoader() : base(typeof(GameFont))
        {

        }

        public override object Load(string path)
        {
            byte[] ttfData = File.ReadAllBytes(path);
            // Larger texture sizes are better for rendering (obviously, less texture swaps) but are much slower to pack new glyphs into (due to the packing algorithm).
            // In general I think larger textures are the way to go: I'd rather have a large lag spike than constant low framerate, but 1024x1024 is a nice balance.
            var font = SpriteFontPlus.DynamicSpriteFont.FromTtf(ttfData, 32f, 1024, 1024);
            font.Blur = 0;

            GameFont gf = new GameFont(font);

            return gf;
        }
    }
}
