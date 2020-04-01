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
            var font = SpriteFontPlus.DynamicSpriteFont.FromTtf(ttfData, 32f);

            font.Blur = 0;

            GameFont gf = new GameFont();
            gf.font = font;

            return gf;
        }
    }
}
