using Microsoft.Xna.Framework;

namespace RazeUI.Entities
{
    public abstract class UIEntity
    {
        /// <summary>
        /// This entity's position offset relative to it's anchored position.
        /// </summary>
        public Point Offset;

        /// <summary>
        /// The size of this entity.
        /// The size can be modified, but will be overwritten if 
        /// </summary>
        public Point Size
        {
            get
            {
                return realSize;
            }
            set
            {
                int inX = value.X;
                int inY = value.Y;

                if (inX < 0)
                    inX = 0;
                if (inY < 0)
                    inY = 0;

                if (!ExpandWidth)
                    realSize.X = inX;

                if (!ExpandHeight)
                    realSize.Y = inY;
            }
        }

        /// <summary>
        /// The parent entity of this. The anchor is relative to the parent's position.
        /// </summary>
        public UIEntity Parent { get; protected internal set; }

        /// <summary>
        /// The anchor of this entity relative to it's parent.
        /// </summary>
        public Anchor Anchor { get; set; }

        public virtual bool ExpandWidth { get; set; } = false;
        public virtual bool ExpandHeight { get; set; } = false;

        public object UserData;

        private Point realSize;

        public T GetUserData<T>()
        {
            if (UserData == null)
                return default;

            return (T)UserData;
        }
    }
}
