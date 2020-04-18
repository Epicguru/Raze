using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;
using RazeContent;
using RazeUI;

namespace Raze.Screens
{
    public class ScreenManager
    {
        public GameScreen CurrentScreen { get; private set; }
        public bool IsTransitioning { get; private set; }

        // Effects settings.
        public float FadeTime = 0.5f;

        private readonly Dictionary<Type, GameScreen> registered = new Dictionary<Type, GameScreen>();
        private readonly Dictionary<string, GameScreen> typeNameRegistered = new Dictionary<string, GameScreen>();
        private float fadeLerp;
        private GameScreen toTransitionTo;
        private GameScreen loading;
        private bool hasStartedLoading;
        private float inLoadAlpha;

        internal void Init(GameScreen gs)
        {
            gs.Load();
            gs.IsActive = true;
            gs.UponShow();
            CurrentScreen = gs;
        }

        public void Register(GameScreen gs)
        {
            if (gs == null)
            {
                Debug.Error("Cannot register null game screen!");
                return;
            }

            Type t = gs.GetType();
            string name = t.FullName;

            if (registered.ContainsKey(t))
            {
                Debug.Error($"{gs} is already registered!");
                return;
            }

            if (typeNameRegistered.ContainsKey(name))
            {
                Debug.Error($"The GameScreen {gs} has the same full name ({name}) as screen {typeNameRegistered[name]}! One of the mod authors will have to change the name of their class or namespace!");
                return;
            }

            registered.Add(t, gs);
            typeNameRegistered.Add(name, gs);

            gs.Manager = this;

            Debug.Trace($"Registered GameScreen: {gs} ({name})");
        }

        public void UnRegister(GameScreen gs)
        {
            if (gs == null)
            {
                Debug.Error("Cannot un-register null game screen!");
                return;
            }

            Type t = gs.GetType();
            string name = t.FullName;

            if (!registered.ContainsKey(t))
            {
                Debug.Error($"{gs} is not registered!");
                return;
            }

            registered.Remove(t);
            typeNameRegistered.Remove(name);

            gs.Manager = null;

            Debug.Trace($"Un-registered GameScreen: {gs}");
        }

        public void Shutdown()
        {
            if(CurrentScreen != null)
            {
                CurrentScreen.Unload();
                CurrentScreen.UponHide();
            }
        }

        public GameScreen GetScreen(string typeName)
        {
            if (typeName == null)
                return null;

            if (typeNameRegistered.ContainsKey(typeName))
                return typeNameRegistered[typeName];
            else
                return null;
        }

        public T GetScreen<T>() where T : GameScreen
        {
            return GetScreen(typeof(T)) as T;
        }

        public GameScreen GetScreen(Type screenType)
        {
            if (screenType == null)
                return null;

            if (registered.ContainsKey(screenType))
                return registered[screenType];
            else
                return null;
        }

        public bool ChangeScreen(string gsTypeName)
        {
            var gs = GetScreen(gsTypeName);
            if (gs == null)
            {
                Debug.Error($"There is no registered GameScreen for type name {gsTypeName}.");
                return false;
            }

            return ChangeScreen(gs);
        }

        public bool ChangeScreen<T>() where T : GameScreen
        {
            var gs = GetScreen(typeof(T));
            if (gs == null)
            {
                Debug.Error($"There is no registered GameScreen for type {typeof(T).FullName}.");
                return false;
            }

            return ChangeScreen(gs);
        }

        public bool ChangeScreen(Type gsType)
        {
            var gs = GetScreen(gsType);
            if(gs == null)
            {
                Debug.Error($"There is no registered GameScreen for type {gsType.FullName}.");
                return false;
            }

            return ChangeScreen(gs);
        }

        private bool ChangeScreen(GameScreen next)
        {
            if(next == null)
            {
                Debug.Error("Cannot change GameScreen to null!");
                return false;
            }

            if(next == CurrentScreen)
            {
                Debug.Error($"{next} is already active!");
                return false;
            }

            if(next == toTransitionTo)
            {
                Debug.Error($"{next} is already queued to be loaded.");
                return false;
            }

            if(next == loading)
            {
                Debug.Error($"{next} is already loading.");
                return false;
            }

            toTransitionTo = next;
            return true;
        }

        public void Update()
        {
            if(toTransitionTo != null && !IsTransitioning)
            {
                IsTransitioning = true;
                loading = toTransitionTo;
                toTransitionTo = null;
                hasStartedLoading = false;
            }

            if(IsTransitioning && !hasStartedLoading && fadeLerp == 1f)
            {
                hasStartedLoading = true;

                // Tell the old screen that it's time is up.
                CurrentScreen.UponHide();
                CurrentScreen.IsActive = false;

                // Unload the old screens' stuff.
                CurrentScreen.Unload();

                // Load. This will block. Another thread will do the simple rendering in the meantime.
                Time.ForceNormalTime();
                // TODO load on another thread.
                inLoadAlpha = -0.5f; // Hide it for a second or two, then fade in loading screen.
                loading.Load();
                

                // Loading is now complete, in future this will be elsewhere because of thread.

                // Tell the new screen that it's in.
                CurrentScreen = loading;
                loading.IsActive = true;
                loading.UponShow();

                Debug.Trace($"Changed screen to {CurrentScreen}, showing...");

                // Now un-fade.
                loading = null;
            }

            if (IsTransitioning)
            {
                if(loading != null)
                {
                    // Fade in.
                    fadeLerp += 1f / FadeTime * Time.unscaledDeltaTime;
                }
                else
                {
                    // Fade out.
                    fadeLerp -= 1f / FadeTime * Time.unscaledDeltaTime;
                }

                // Clamp lerp.
                fadeLerp = MathHelper.Clamp(fadeLerp, 0f, 1f);

                // Check if the fade in is complete...
                if(fadeLerp == 0 && loading == null)
                {
                    // Done! No longer transitioning.
                    IsTransitioning = false;
                }
            }

            CurrentScreen?.Update();
        }

        public void Draw(SpriteBatch spr)
        {
            CurrentScreen?.Draw(spr);
        }

        public void DrawUI(SpriteBatch spr, LayoutUserInterface ui)
        {
            CurrentScreen?.DrawUI(spr, ui);

            // Draw fade thing.
            ui.UI.GlobalTint = new Color(0, 0, 0, fadeLerp);
        }

        public void DrawUIBackupThread(SpriteBatch spr, float dt)
        {
            inLoadAlpha += dt * 0.75f;

            float a = MathHelper.Clamp(inLoadAlpha, 0f, 1f);
            Color tint = new Color(1f, 1f, 1f, a);

            string toDraw = loading?.LoadingScreenText;
            if(toDraw != null)
            {
                Point size = Main.MediumFont.MeasureString(toDraw);
                spr.DrawString(Main.MediumFont, toDraw, new Vector2((Screen.Width - size.X) * 0.5f, (Screen.Height - size.Y) * 0.5f + 80f), tint);
            }

            // Advance the frames on this loading icon.
            Main.LoadingIconSprite.ChangeFrame(1);
            spr.Draw(Main.LoadingIconSprite, new Vector2(Screen.Width * 0.5f, Screen.Height * 0.5f), tint, 0f);
        }
    }
}
