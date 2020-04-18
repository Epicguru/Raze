using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace RazeUI.Providers.Implementations
{
    public class MonoGameKeyboardProvider : IKeyboardProvider
    {
        public GameWindow Window
        {
            get
            {
                return window;
            }
            set
            {
                if (value == window)
                    return;

                if (window != null)
                {
                    window.KeyUp -= SendKeyUp;
                    window.KeyDown -= SendKeyDown;
                    window.TextInput -= SendTextInput;
                }

                window = value;
                window.KeyUp += SendKeyUp;
                window.KeyDown += SendKeyDown;
                window.TextInput += SendTextInput;
            }
        }

        private GameWindow window;

        public MonoGameKeyboardProvider(GameWindow window)
        {
            this.Window = window;
        }

        public void SendTextInput(object _, TextInputEventArgs e)
        {
            OnKeyTyped?.Invoke(e.Key, e.Character);
        }

        public void SendKeyDown(object _, InputKeyEventArgs e)
        {
            OnKeyDown?.Invoke(e.Key);
        }

        public void SendKeyUp(object _, InputKeyEventArgs e)
        {
            OnKeyUp?.Invoke(e.Key);
        }

        public event KeyboardEvent OnKeyTyped;
        public event Action<Keys> OnKeyDown;
        public event Action<Keys> OnKeyUp;
    }
}
