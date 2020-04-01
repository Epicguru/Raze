using GVS.Screens;
using GVS.Screens.Instances;
using GVS.Sprites;
using GVS.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

// Allow GVS Tests to access 'internal' methods, fields etc.
[assembly: InternalsVisibleTo("GVS_Tests")]

namespace GVS
{
    public class Main : Game
    {
        public static ContentManager ContentManager
        {
            get { return main.Content; }
        }
        public static GraphicsDeviceManager Graphics;
        public static GraphicsDevice GlobalGraphicsDevice;
        public static GameWindow GameWindow;
        public static SpriteBatch SpriteBatch;
        public static Camera Camera;
        public static SpriteFont MediumFont;
        public static Sprite MissingTextureSprite;

        // TODO fix this, need a better way for each tile to load content before packing atlas.
        public static Sprite GrassTile, MountainTile, TreeTile, StoneTile, StoneTopTile;
        public static Sprite WaterTile, SandTile, HouseTile;
        public static Sprite TileShadowTopLeft, TileShadowTopRight, TileShadowBottomLeft, TileShadowBottomRight;

        public static AnimatedSprite LoadingIconSprite { get; private set; }

        public static ScreenManager ScreenManager { get; private set; }
        public static TileAtlas SpriteAtlas { get; private set; }
        public static IsoMap Map { get; internal set; }
        public static Process GameProcess
        {
            get
            {
                return main.thisProcess;
            }
        }

        public static string ContentDirectory { get; private set; }
        public static Rectangle ClientBounds { get; private set; }

        private static Main main;
        private static float loadIconTimer;

        private Process thisProcess;

        public Main()
        {
            main = this;
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            GameWindow = base.Window;
            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            thisProcess = Process.GetCurrentProcess();
        }

        internal static void ForceExitGame()
        {
            main.Exit();
        }

        protected override void Update(GameTime gameTime)
        {
            ClientBounds = Window.ClientBounds;
        }

        protected override void Initialize()
        {
            Thread.CurrentThread.Name = "Monogame Thread";
            Main.GlobalGraphicsDevice = base.GraphicsDevice;

            // Create camera.
            Camera = new Camera();
            Camera.Zoom = 0.5f;

            // Create screen manager.
            ScreenManager = new ScreenManager();

            // Register some screens.
            ScreenManager.Register(new SplashScreen());
            ScreenManager.Register(new PlayScreen());
            ScreenManager.Register(new MainMenuScreen());
            ScreenManager.Register(new ConnectScreen());

            ScreenManager.Init(ScreenManager.GetScreen<SplashScreen>());

            // Init debug.
            Debug.Init();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Find content directory...
            ContentDirectory = Path.Combine(Environment.CurrentDirectory, Content.RootDirectory);

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // Load some default fonts.
            MediumFont = Content.Load<SpriteFont>("Fonts/MediumFont");

            // Load loading icon atlas.
            LoadingIconSprite = new AnimatedSprite(Content.Load<Texture2D>("Textures/LoadingIconAtlas"), 128, 128, 60);
            LoadingIconSprite.Pivot = new Vector2(0.5f, 0.5f);

            // Create the main sprite atlas.
            SpriteAtlas = new TileAtlas(1024, 1024);

            // Loading missing texture sprite.
            MissingTextureSprite = SpriteAtlas.Add("Textures/MissingTexture");
            MissingTextureSprite.Pivot = new Vector2(0.5f, 1f); // Bottom center.

            // Temporarily load tiles here.
            GrassTile = SpriteAtlas.Add("Textures/GrassTile");
            MountainTile = SpriteAtlas.Add("Textures/Mountain");
            TreeTile = SpriteAtlas.Add("Textures/Trees");
            StoneTile = SpriteAtlas.Add("Textures/StoneTile");
            StoneTopTile = SpriteAtlas.Add("Textures/StoneTop");
            WaterTile = SpriteAtlas.Add("Textures/WaterTile");
            SandTile = SpriteAtlas.Add("Textures/SandTile");
            TileShadowTopRight = SpriteAtlas.Add("Textures/TileShadowTopRight");
            TileShadowTopLeft = SpriteAtlas.Add("Textures/TileShadowTopLeft");
            TileShadowBottomRight = SpriteAtlas.Add("Textures/TileShadowBottomRight");
            TileShadowBottomLeft = SpriteAtlas.Add("Textures/TileShadowBottomLeft");
            HouseTile = SpriteAtlas.Add("Textures/HouseTile");

            SpriteAtlas.Pack(false);

            Loop.Start();           
        }

        protected override void UnloadContent()
        {
            Debug.Log("Unloading content...");
        }

        protected override void EndRun()
        {
            Loop.StopAndWait();
            ScreenManager.Shutdown();
            Debug.Shutdown();
            thisProcess.Dispose();
            thisProcess = null;
            base.EndRun();
        }

        protected override void EndDraw()
        {
            // Do not present the device to the screen, this is handled in the Loop class.
        }

        internal static void MainUpdate()
        {
            // Update loading icon.
            loadIconTimer += Time.unscaledDeltaTime;
            const float INTERVAL = 1f / 60f;
            while(loadIconTimer >= INTERVAL)
            {
                LoadingIconSprite.ChangeFrame(1, true);
                loadIconTimer -= INTERVAL;
            }

            ScreenManager.Update();
        }

        internal static void MainDraw()
        {
            ScreenManager.Draw(SpriteBatch);
        }

        internal static void MainDrawUI()
        {
            ScreenManager.DrawUI(SpriteBatch);
        }
    }
}