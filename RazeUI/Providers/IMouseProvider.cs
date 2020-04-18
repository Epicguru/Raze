
using Microsoft.Xna.Framework;

namespace RazeUI.Providers
{
    public interface IMouseProvider
    {
        public virtual void PreDraw()
        {

        }
        public Point GetMousePos();
        public bool IsLeftMouseDown();
        public bool IsRightMouseDown();
        public bool IsLeftMouseClick();
        public bool IsRightMouseClick();
    }
}
