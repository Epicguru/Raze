using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Raze.Sprites
{
    public class AnimatedSprite : Sprite
    {
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int TotalFrames { get; }
        public int ColumnCount { get; private set; }
        public int CurrentFrameIndex { get; private set; }

        public AnimatedSprite(Texture2D texture, int width, int height, int frames) : base(texture, new Rectangle(0, 0, width, height))
        {
            this.FrameWidth = width;
            this.FrameHeight = height;
            this.TotalFrames = frames;

            CalculateColumnCount();
        }

        public void ChangeFrame(int offset, bool wrap = true)
        {
            if (offset == 0)
                return;

            int target = CurrentFrameIndex + offset;
            int real = wrap ? target % TotalFrames : target;

            this.SetFrame(real);
        }

        public void SetFrame(int index)
        {
            if(index < 0 || index >= TotalFrames)
            {
                Debug.Warn($"Frame index out of bounds: {index}. Min: 0, max: {TotalFrames - 1} inclusive.");
                return;
            }

            if (ColumnCount == 0)
                return;

            CurrentFrameIndex = index;
            base.Region = GetFrameBounds(index);
        }

        public Rectangle GetFrameBounds(int frameIndex)
        {
            if (this.ColumnCount == 0)
                return Rectangle.Empty;

            if (frameIndex < 0 || frameIndex >= TotalFrames)
                return Rectangle.Empty;

            int ix = frameIndex % ColumnCount;
            int iy = frameIndex / ColumnCount;

            return new Rectangle(ix * FrameWidth, iy * FrameHeight, FrameWidth, FrameHeight);
        }

        private void CalculateColumnCount()
        {
            if (this.Texture == null || this.Texture.IsDisposed)
                return;

            int w = Texture.Width;

            this.ColumnCount = w / FrameWidth;
        }

        public override void SetTexture(Texture2D t)
        {
            base.SetTexture(t);
            CalculateColumnCount();
        }
    }
}
