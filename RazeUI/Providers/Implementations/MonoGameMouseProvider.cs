using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RazeUI.Providers.Implementations
{
    public class MonoGameMouseProvider : IMouseProvider
    {
        private MouseState state, lastState;

        public void PreDraw()
        {
            lastState = state;
            state = Mouse.GetState();
        }

        public Point GetMousePos()
        {
            return state.Position;
        }

        public bool IsLeftMouseDown()
        {
            return state.LeftButton == ButtonState.Pressed;
        }

        public bool IsRightMouseDown()
        {
            return state.RightButton == ButtonState.Pressed;
        }

        public bool IsLeftMouseClick()
        {
            return IsLeftMouseDown() && lastState.LeftButton != ButtonState.Pressed;
        }

        public bool IsRightMouseClick()
        {
            return IsRightMouseDown() && lastState.RightButton != ButtonState.Pressed;
        }
    }
}
