using GVS.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GVS
{
    /// <summary>
    /// A static class that collects all of XNA's input utilities into one place.
    /// Mouse, Keyboard and (possibly in the future) Gamepad inputs are all available here.
    /// Input is polled and updated on a per-frame basis, so input state will not change in the middle of
    /// a frame execution. Has various extra features, such as disabling input when necessary, and detecting when the
    /// mouse exits the game window.
    /// </summary>
    public static class Input
    {
        public static bool Enabled { get; set; } = true;

        public static Point MousePos { get; private set; }
        public static Tile TileUnderMouse
        {
            get
            {
                return tileUnderMouse?.Map == null ? null : tileUnderMouse;
            }
        }
        public static Vector2 MouseWorldPos { get; private set; }
        public static bool MouseInWindow { get; private set; }
        public static int MouseScroll { get; private set; }
        public static int MouseScrollDelta { get; private set; }

        private static Tile tileUnderMouse;
        private static KeyboardState currentKeyState;
        private static KeyboardState lastKeyState;

        private static MouseState currentMouseState;
        private static MouseState lastMouseState;

        public static void StartFrame()
        {
            lastKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();

            lastMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            MousePos = currentMouseState.Position;
            MouseWorldPos = Main.Camera.ScreenToWorldPosition(MousePos.ToVector2());
            MouseInWindow = Screen.Contains(MousePos.X, MousePos.Y);

            MouseScrollDelta = currentMouseState.ScrollWheelValue - MouseScroll;
            MouseScroll = currentMouseState.ScrollWheelValue;

            tileUnderMouse = GetTileFromWorldPosition(MouseWorldPos);
        }

        /// <summary>
        /// Returns true every frame while the key is held down.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the key is being held down and input is active.</returns>
        public static bool IsKeyDown(Keys key)
        {
            return Enabled && Pressed(currentKeyState[key]);
        }

        /// <summary>
        /// Returns true on the first frame that the key is pressed.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the key has just been pressed and input is active.</returns>
        public static bool IsKeyJustDown(Keys key)
        {
            return Enabled && Down(currentKeyState[key], lastKeyState[key]);
        }

        /// <summary>
        /// Returns true on the first frame that the key has just been released after being held down.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the key has just been released and input is active.</returns>
        public static bool IsKeyJustUp(Keys key)
        {
            return Enabled && Up(currentKeyState[key], lastKeyState[key]);
        }

        private static bool MousePressed(ButtonState s)
        {
            return Enabled && s == ButtonState.Pressed;
        }

        private static bool MouseDown(ButtonState current, ButtonState last)
        {
            return Enabled && current == ButtonState.Pressed && last == ButtonState.Released;
        }

        private static bool MouseUp(ButtonState current, ButtonState last)
        {
            return Enabled && current == ButtonState.Released && last == ButtonState.Pressed;
        }

        public static bool RightMousePressed()
        {
            return MousePressed(currentMouseState.RightButton);
        }

        public static bool LeftMousePressed()
        {
            return MousePressed(currentMouseState.LeftButton);
        }

        public static bool MiddleMousePressed()
        {
            return MousePressed(currentMouseState.MiddleButton);
        }

        public static bool RightMouseDown()
        {
            return MouseDown(currentMouseState.RightButton, lastMouseState.RightButton);
        }

        public static bool LeftMouseDown()
        {
            return MouseDown(currentMouseState.LeftButton, lastMouseState.LeftButton);
        }

        public static bool MiddleMouseDown()
        {
            return MouseDown(currentMouseState.MiddleButton, lastMouseState.MiddleButton);
        }

        public static bool RightMouseUp()
        {
            return MouseUp(currentMouseState.RightButton, lastMouseState.RightButton);
        }

        public static bool LeftMouseUp()
        {
            return MouseUp(currentMouseState.LeftButton, lastMouseState.LeftButton);
        }

        public static bool MiddleMouseUp()
        {
            return MouseUp(currentMouseState.MiddleButton, lastMouseState.MiddleButton);
        }

        private static bool Pressed(KeyState s)
        {
            return s == KeyState.Down;
        }

        private static bool Down(KeyState current, KeyState last)
        {
            return current == KeyState.Down && last == KeyState.Up;
        }

        private static bool Up(KeyState current, KeyState last)
        {
            return current == KeyState.Up && last == KeyState.Down;
        }

        public static void EndFrame()
        {

        }

        private static Tile GetTileFromWorldPosition(Vector2 flatWorldPosition)
        {
            if (Main.Map == null)
                return null;

            int maxZ = Main.Map.Height - 1; // The maximum Z to consider. Allow for selection when top layers are hidden.

            Vector2 pos = Main.Map.GetGroundPositionFromWorldPosition(flatWorldPosition, out IsoMap.TileSide side);
            Point groundPos = new Point((int)pos.X, (int)pos.Y);

            // Identify the columns to consider.
            Point topA = new Point(groundPos.X, groundPos.Y);
            Point topB = side == IsoMap.TileSide.Right ? topA + new Point(0, 1) : topA + new Point(1, 0);

            // Start from bottom upwards, top to down, B then A.
            // First tile to 'collide' is the selected tile.

            // How far down the columns should we go?
            int downA = maxZ;
            int downB = maxZ - 1;

            Point bottomA = topA + new Point(downA, downA);
            Point bottomB = topB + new Point(downB, downB);

            Point bPoint = bottomB;
            Point aPoint = bottomA;
            int i = 0;
            var map = Main.Map;
            while (true)
            {
                // Start with B, then A, then B...
                Tile t;

                // A! Two tiles to check!
                int aZ = maxZ + 1 - i; // This is out of bounds when i = 0, because of the way the universe works.
                if (aZ <= maxZ)
                {
                    t = map.GetTile(aPoint.X, aPoint.Y, aZ);
                    if (IsSelectable(t))
                        return t;
                }
                aZ = maxZ - i; // This will also be out of bounds on the last iteration.
                if (aZ < 0)
                    return null; // Quit, tile not found anywhere.

                t = map.GetTile(aPoint.X, aPoint.Y, aZ);
                if (IsSelectable(t))
                    return t;

                // B!
                int bZ = maxZ - i;
                if (bZ != 0)
                {
                    t = map.GetTile(bPoint.X, bPoint.Y, bZ);
                    if (IsSelectable(t))
                        return t;
                }

                // Increase counter and move the two points upwards.
                i++;
                aPoint -= new Point(1, 1);
                bPoint -= new Point(1, 1);
            }

            bool IsSelectable(Tile t)
            {
                return t != null;
            }
        }
    }
}
