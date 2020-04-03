using Microsoft.Xna.Framework;

namespace RazeUI
{
    internal struct UIContext
    {
        public Rectangle Bounds;
        public MarginData Margins;

        public Rectangle LastDrawn
        {
            get
            {
                return lastDrawn;
            }
            set
            {
                UpdateLastDrawn(value);
            }
        }
        public Anchor LastAnchor;
        public int LastFontSize;
        public bool LastExpandWidth;
        public int LowestY;

        private Rectangle lastDrawn;

        private void UpdateLastDrawn(Rectangle bounds)
        {
            int lowestY = bounds.Y + bounds.Height;
            if (lowestY > LowestY)
                LowestY = lowestY;

            this.lastDrawn = bounds;
        }
    }
}
