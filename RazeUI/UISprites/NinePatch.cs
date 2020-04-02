using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace RazeUI.UISprites
{
    public class NinePatch
    {
        public UISprite TopLeft, TopCenter, TopRight;
        public UISprite CenterLeft, Center, CenterRight;
        public UISprite BottomLeft, BottomCenter, BottomRight;

        public NinePatch(UISprite all, Point centerStart, Point centerSize)
        {
            var tex = all.Texture;
            var stp = all.Region.Location;

            int left = centerStart.X;
            int right = all.Region.Size.X - (centerStart.X + centerSize.X);
            int top = centerStart.Y;
            int bottom = all.Region.Size.Y - (centerStart.Y + centerSize.Y);

            UISprite topLeft = new UISprite(tex, new Rectangle(stp, centerStart));
            UISprite topRight = new UISprite(tex, new Rectangle(stp - new Point(right, 0), new Point(right, top)));
            UISprite bottomLeft = new UISprite(tex, new Rectangle(new Point(stp.X, stp.Y + centerStart.Y + centerSize.Y), new Point(left, bottom)));
            UISprite bottomRight = new UISprite(tex, new Rectangle(new Point(stp.X + centerStart.X + centerSize.X, stp.Y + centerStart.Y + centerSize.Y), new Point(right, bottom)));

            UISprite center = new UISprite(tex, new Rectangle(stp + centerStart, centerSize));

            UISprite centerLeft = new UISprite(tex, new Rectangle(stp + new Point(0, centerStart.Y), new Point(left, centerSize.Y)));
            UISprite centerRight = new UISprite(tex, new Rectangle(stp + new Point(centerStart.X + centerSize.X, centerStart.Y), new Point(right, centerSize.Y)));

            UISprite topCenter = new UISprite(tex, new Rectangle(stp + new Point(centerStart.X, 0), new Point(centerSize.X, top)));
            UISprite bottomCenter = new UISprite(tex, new Rectangle(stp + new Point(centerStart.X, centerStart.Y + centerSize.Y), new Point(centerSize.X, bottom)));

            SetParts(topLeft, topCenter, topRight, centerLeft, center, centerRight, bottomLeft, bottomCenter, bottomRight);
        }

        public NinePatch(UISprite topLeft, UISprite topCenter, UISprite topRight, UISprite centerLeft, UISprite center, UISprite centerRight, UISprite bottomLeft, UISprite bottomCenter, UISprite bottomRight)
        {
            SetParts(topLeft, topCenter, topRight, centerLeft, center, centerRight, bottomLeft, bottomCenter, bottomRight);
        }

        public void SetParts(UISprite topLeft, UISprite topCenter, UISprite topRight, UISprite centerLeft, UISprite center, UISprite centerRight, UISprite bottomLeft, UISprite bottomCenter, UISprite bottomRight)
        {
            this.TopLeft = topLeft;
            this.CenterLeft = centerLeft;
            this.BottomLeft = bottomLeft;

            this.TopCenter = topCenter;
            this.Center = center;
            this.BottomCenter = bottomCenter;

            this.TopRight = topRight;
            this.CenterRight = centerRight;
            this.BottomRight = bottomRight;
        }

        public void Draw(SpriteBatch spr, Vector2 position, Vector2 size, Color color)
        {
            if (spr == null)
                throw new ArgumentNullException(nameof(spr));

            Point pos = new Point((int) position.X, (int) position.Y);
            Point pSize = new Point((int) size.X, (int) size.Y);

            if (pSize.X < 0)
                pSize.X = 0;
            if (pSize.Y < 0)
                pSize.Y = 0;

            int minWidth = TopLeft.Region.Width + TopRight.Region.Width;
            int innerWidth = pSize.X - minWidth;
            if (innerWidth < 0)
                innerWidth = 0;

            int minHeight = TopLeft.Region.Height + TopRight.Region.Height;
            int innerHeight = pSize.Y - minHeight;
            if (innerHeight < 0)
                innerHeight = 0;

            // Top left.
            TopLeft.Draw(spr, pos, color);

            // Top center.
            if(innerWidth != 0)
                TopCenter.Draw(spr, new Rectangle(pos + new Point(TopLeft.Region.Width, 0), new Point(innerWidth, TopCenter.Region.Height)), color);

            // Top right.
            TopRight.Draw(spr, pos + new Point(TopLeft.Region.Width, 0), color);

            // Center left.
            if(innerHeight != 0)
                CenterLeft.Draw(spr, new Rectangle(pos + new Point(0, TopLeft.Region.Height), new Point(TopLeft.Region.Width, innerHeight)), color);

            // Center.
            if (innerWidth != 0 && innerHeight != 0)
                Center.Draw(spr, new Rectangle(pos + TopLeft.Region.Size, new Point(innerWidth, innerHeight)), color);

            // Center right.
            if (innerHeight != 0)
                CenterRight.Draw(spr, new Rectangle(pos + new Point(TopLeft.Region.Width + innerWidth, TopLeft.Region.Height), new Point(CenterRight.Region.Width, innerHeight)), color);

            // Bottom left.
            BottomLeft.Draw(spr, pos + new Point(0, TopLeft.Region.Height + innerHeight), color);

            // Bottom center.
            if(innerWidth != 0)
                BottomCenter.Draw(spr, new Rectangle(pos + new Point(TopLeft.Region.Width, TopLeft.Region.Height + innerHeight), new Point(innerWidth, BottomCenter.Region.Height)), color);

            // Bottom right.
            BottomRight.Draw(spr, pos + new Point(innerWidth, innerHeight) + TopLeft.Region.Size, color);
        }
    }
}
