using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Raze.Sprites
{
    // URGTODO make re-packable
    public class SpriteAtlas : IDisposable
    {
        public SortMode SortingMode { get; set; } = SortMode.Height;
        public SortDirection SortingDirection { get; set; } = SortDirection.Descending;
        public int Width { get; }
        public int Height { get; }
        public int Padding
        {
            get
            {
                return padding;
            }
            set
            {
                if (value < 0)
                    Debug.Error($"Cannot set the value of padding to {value}! Must be at least 0.");
                else
                    padding = value;

            }
        }
        public Texture2D Texture { get; private set; }
        public bool IsDirty { get; private set; }

        private int padding = 1;
        private HashSet<string> texturePaths = new HashSet<string>();
        private Dictionary<string, Sprite> packedSprites = new Dictionary<string, Sprite>();
        private List<SpriteWrapper> prePacked = new List<SpriteWrapper>();
        private Dictionary<int, Color[]> colorCaches = new Dictionary<int, Color[]>();
        private PackerRegion rootRegion;

        public enum SortMode
        {
            Width,
            Height,
            MaxWidthHeight
        }

        public enum SortDirection
        {
            Descending,
            Ascending
        }

        private class SpriteWrapper : IComparable<SpriteWrapper>
        {
            public readonly Sprite Sprite;
            public Texture2D Texture;

            private readonly SpriteAtlas atlas;

            public int Width { get { return Texture.Width; } }
            public int Height { get { return Texture.Height; } }
            public int MaxLength { get { return MathHelper.Max(Width, Height); } }

            public SpriteWrapper(SpriteAtlas a, Texture2D tex, Sprite sprite)
            {
                this.atlas = a;
                this.Texture = tex;
                this.Sprite = sprite;
            }

            public int CompareTo(SpriteWrapper other)
            {
                int compare;

                switch (atlas.SortingMode)
                {
                    case SortMode.Width:
                        compare = other.Width - this.Width;
                        break;
                    case SortMode.Height:
                        compare = other.Height - this.Height;
                        break;
                    case SortMode.MaxWidthHeight:
                        compare = other.MaxLength - this.MaxLength;
                        break;
                    default:
                        compare = 0;
                        break;
                }

                if (atlas.SortingDirection == SortDirection.Ascending)
                    compare = -compare;

                return compare;
            }
        }

        private class PackerRegion : IDisposable
        {
            public Rectangle Bounds;
            public bool Used = false;
            public PackerRegion Down, Right;

            public PackerRegion(Rectangle bounds)
            {
                this.Bounds = bounds;
            }

            public void Dispose()
            {
                Down?.Dispose();
                Down = null;
                Right?.Dispose();
                Right = null;
            }
        }

        public SpriteAtlas(int width, int height)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
            if(height < 1)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");

            this.Width = width;
            this.Height = height;
        }

        public Sprite Add(string contentPath)
        {
            return Add(Main.ContentManager.Load<Texture2D>(contentPath));
        }

        public Sprite Add(Texture2D texture)
        {
            if (texture == null)
                return null;

            string name = texture.Name;
            foreach (var pp in prePacked)
            {
                if(pp.Texture == texture)
                {
                    //Debug.Warn($"Texture {name} has already been added for packing into this atlas!");
                    return pp.Sprite;
                }
            }

            Sprite newSprite = new Sprite(null, Rectangle.Empty);
            prePacked.Add(new SpriteWrapper(this, texture, newSprite));

            IsDirty = true;

            return newSprite;
        }

        private bool TryFit(int width, int height, out Point packedPosition)
        {
            var node = FindRegion(rootRegion, width, height);
            if (node == null)
            {
                packedPosition = Point.Zero;
                return false;
            }
            else
            {
                packedPosition = SplitRegion(node, width, height);
                return true;
            }
        }

        private PackerRegion FindRegion(PackerRegion region, int w, int h)
        {
            if (region.Used)
            {
                var right = this.FindRegion(region.Right, w, h);
                if (right != null)
                    return right;
                var down = this.FindRegion(region.Down, w, h);
                return down;
            }

            if (w <= region.Bounds.Width && h <= region.Bounds.Height)
            {
                return region;
            }

            return null;
        }

        private Point SplitRegion(PackerRegion region, int w, int h)
        {
            Debug.Assert(region != null, "Region is null, cannot split it!");
            Debug.Assert(region != null && !region.Used, "Region is already used!");

            region.Used = true;
            region.Down = new PackerRegion(new Rectangle(region.Bounds.X, region.Bounds.Y + h, region.Bounds.Width, region.Bounds.Height - h));
            region.Right = new PackerRegion(new Rectangle(region.Bounds.X + w, region.Bounds.Y, region.Bounds.Width - w, h));
            region.Bounds.Width = w;
            region.Bounds.Height = h;

            return region.Bounds.Location;
        }

        public void Pack(bool allowUnpackedSprites)
        {
            if (!IsDirty)
            {
                Debug.Warn("Sprite atlas is not dirty, will not repack.");
                return;
            }
            IsDirty = false;

            packedSprites.Clear();
            if (prePacked.Count == 0)
                return;

            // Go!
            Debug.StartTimer("Pack tiles atlas");
            int packedCount = prePacked.Count;
            Debug.Log($"Packing the sprite atlas, {packedCount} sprites in a {Width}x{Height} texture.");
            Debug.Log($"Sprite sorting: {SortingMode} ({SortingDirection})");

            // Sort the pre-packed sprites by longest length, decreasing.
            prePacked.Sort();

            // Create the texture.
            Texture2D tex = new Texture2D(Main.GlobalGraphicsDevice, Width, Height, false, SurfaceFormat.Color);

            // Try to pack all of those sprites into the atlas.
            rootRegion = new PackerRegion(new Rectangle(0, 0, Width, Height));
            foreach (SpriteWrapper pp in prePacked)
            {
                bool canFit = this.TryFit(pp.Width + Padding * 2, pp.Height + Padding * 2, out Point packedPos);
                if (!canFit)
                {
                    Debug.Error($"Failed to pack {pp.Texture.Name} into the atlas!");
                    if (allowUnpackedSprites)
                    {
                        pp.Sprite.SetTexture(pp.Texture);
                        pp.Sprite.Region = new Rectangle(0, 0, pp.Texture.Width, pp.Texture.Height);
                        pp.Sprite.Name = pp.Texture.Name;
                        Debug.Warn("The sprite will still function, but will have greatly decreased performance.");
                    }
                    else
                    {
                        pp.Sprite.SetTexture(null);
                        pp.Sprite.Name = pp.Texture.Name;
                        pp.Sprite.Region = new Rectangle(0, 0, pp.Texture.Width, pp.Texture.Height);
                        Debug.Warn("The sprite will be invalid (will show missing texture)");
                    }
                    continue;
                }

                // Write to the texture.
                Blit(pp.Texture, tex, packedPos, Padding);

                // Set up the sprite to have the final data.
                pp.Sprite.SetTexture(tex);
                pp.Sprite.Name = pp.Texture.Name;
                pp.Sprite.Region = new Rectangle(packedPos.X + Padding, packedPos.Y + padding, pp.Texture.Width, pp.Texture.Height);

                // Dispose of the old pre-packed texture...
                pp.Texture.Dispose();
                pp.Texture = null;

                // Add this packed sprite to the list of packed sprites.
                packedSprites.Add(pp.Sprite.Name, pp.Sprite);
            }

            // Clear the pre-packed list.
            prePacked.Clear();

            // Clear the color cache. Used in Blit().
            colorCaches.Clear();

            // Clear the region binary tree. It gets messy.
            rootRegion.Dispose();
            rootRegion = null;

            // Save this texture, and delete old texture.
            if(this.Texture != null)
            {
                this.Texture.Dispose();
            }
            this.Texture = tex;

            Debug.StopTimer(true);
        }

        private void Blit(Texture2D source, Texture2D destination, Point position, int pad)
        {
            // Try to get an existing color array from the cache, to avoid loads of garbage collection
            // and memory usage.
            Color[] sourceColors = GetCachedOrCreate(source.Width * source.Height);
            Color[] widthColors = null;
            Color[] heightColors = null;

            if(Padding >= 0)
            {
                widthColors = GetCachedOrCreate(source.Width * pad);
                heightColors = GetCachedOrCreate(source.Height * pad);
            }

            // Read the source data into the color array.
            source.GetData(sourceColors);

            // Adjust position to account for padding.
            if (pad >= 1)
                position += new Point(pad, pad);

            // Write main colors.
            destination.SetData(0, new Rectangle(position.X, position.Y, source.Width, source.Height), sourceColors, 0, sourceColors.Length);

            if(pad >= 0)
            {
                // Left side.
                Color[] side = heightColors;
                for (int x = 0; x < pad; x++)
                {
                    for (int y = 0; y < source.Height; y++)
                    {
                        side[x + y * pad] = sourceColors[y * source.Width];
                    }
                }
                destination.SetData(0, new Rectangle(position.X - pad, position.Y, pad, source.Height), side, 0, side.Length);

                // Right side.
                for (int x = 0; x < pad; x++)
                {
                    for (int y = 0; y < source.Height; y++)
                    {
                        side[x + y * pad] = sourceColors[(source.Width - 1) + y * source.Width];
                    }
                }
                destination.SetData(0, new Rectangle(position.X + source.Width, position.Y, pad, source.Height), side, 0, side.Length);

                // Top side.
                Color[] tops = widthColors;
                for (int x = 0; x < source.Width; x++)
                {
                    for (int y = 0; y < pad; y++)
                    {
                        tops[x + y * source.Width] = sourceColors[x];
                    }
                }
                destination.SetData(0, new Rectangle(position.X, position.Y - pad, source.Width, pad), tops, 0, tops.Length);

                // Bottom side.
                for (int x = 0; x < source.Width; x++)
                {
                    for (int y = 0; y < pad; y++)
                    {
                        tops[x + y * source.Width] = sourceColors[x + (source.Height - 1) * source.Width];
                    }
                }
                destination.SetData(0, new Rectangle(position.X, position.Y + source.Height, source.Width, pad), tops, 0, tops.Length);
            }

            // Local method to get cached colors.
            Color[] GetCachedOrCreate(int length)
            {
                if (colorCaches.ContainsKey(length))
                {
                    return colorCaches[length];
                }
                else
                {
                    Color[] created = new Color[length];
                    colorCaches.Add(length, created);

                    return created;
                }
            }
        }

        public void Dispose()
        {
            if(prePacked != null)
            {
                prePacked.Clear();
                prePacked = null;
            }
            if(colorCaches != null)
            {
                colorCaches.Clear();
                colorCaches = null;
            }
            if (packedSprites != null)
            {
                // Remove the texture from all the sprites, to make sure that the sprites are not
                // keeping the texture in memory.
                foreach (var pair in packedSprites)
                {
                    var sprite = pair.Value;
                    if (sprite != null)
                        sprite.SetTexture(null);
                }
                packedSprites.Clear();
                packedSprites = null;
            }
            if (Texture != null)
            {
                Texture.Dispose();
                Texture = null;
            }
        }
    }
}
