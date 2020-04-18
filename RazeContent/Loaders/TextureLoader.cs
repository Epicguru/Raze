using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace RazeContent.Loaders
{
    public class TextureLoader : ContentLoader
    {
        public override string ExpectedFileExtension => ".png";

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

            // Need to premultiply color.
            Color[] data = new Color[loaded.Width * loaded.Height];
            loaded.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                var cIn = data[i];
                var cOut = Color.FromNonPremultiplied(cIn.ToVector4());

                data[i] = cOut;
            }

            var fi = new FileInfo(path);
            loaded.Name = fi.Name[..^fi.Extension.Length];
            loaded.SetData(data);

            return loaded;
        }
    }
}
