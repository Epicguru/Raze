using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace RazeContent.Loaders
{
    public class TextureLoader : ContentLoader
    {
        public TextureLoader() : base(typeof(Texture2D))
        {

        }

        public override object Load(string path)
        {
            var gd = ContentManager.GraphicsDevice;
            if (gd == null)
                throw new Exception("Cannot load texture, this RazeContentManager's GraphicsDevice is null!");

            using FileStream fs = new FileStream(path, FileMode.Open);

            Texture2D loaded = Texture2D.FromStream(gd, fs);

            return loaded;
        }
    }
}
