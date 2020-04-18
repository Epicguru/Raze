using System;
using Microsoft.Xna.Framework.Input;

namespace RazeUI.Providers
{
    public interface IKeyboardProvider
    {
        event KeyboardEvent OnKeyTyped;
        event Action<Keys> OnKeyDown;
        event Action<Keys> OnKeyUp;
    }
}
