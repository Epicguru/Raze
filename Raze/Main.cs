using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Defs;
using Raze.Screens;
using Raze.Screens.Instances;
using Raze.Sprites;
using Raze.World;
using RazeContent;
using RazeUI;
using RazeUI.Providers.Implementations;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Raze.Entities;
using Raze.World.Tiles;
using Raze.World.Tiles.Components;

// Allow GVS Tests to access 'internal' methods, fields etc.
[assembly: InternalsVisibleTo("GVS_Tests")]

namespace Raze
{
    public class Main : Game
    {
        public static RazeContentManager ContentManager { get; private set; }
        public static SpriteBatch SpriteBatch { get; private set; }
        public static GraphicsDeviceManager Graphics { get; private set; }
        public static GraphicsDevice GlobalGraphicsDevice { get; private set; }
        public static GameWindow GameWindow { get; private set; }
        public static Camera Camera { get; private set; }
        public static GameFont MediumFont { get; private set; }
        public static Sprite MissingTextureSprite { get; private set; }

        // TODO fix this, need a better way for each tile to load content before packing atlas.
        public static Sprite MountainTile, TreeTile;
        public static Sprite HouseTile;
        public static Sprite TileShadowTopLeft, TileShadowTopRight, TileShadowBottomLeft, TileShadowBottomRight;

        public static AnimatedSprite LoadingIconSprite { get; private set; }

        public static DefDatabase DefDatabase { get; private set; }
        public static LayoutUserInterface LayoutUI { get; private set; }
        public static ScreenManager ScreenManager { get; private set; }
        public static SpriteAtlas SpriteAtlas { get; private set; }
        public static IsoMap Map { get; internal set; }
        public static Process GameProcess
        {
            get
            {
                return Instance.thisProcess;
            }
        }

        public static string ContentDirectory { get; private set; }
        public static Rectangle ClientBounds { get; private set; }

        public static Main Instance { get; private set; }
        private static float loadIconTimer;

        private Process thisProcess;

        public Main()
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            GameWindow = base.Window;
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            thisProcess = Process.GetCurrentProcess();
        }

        public static void ForceExitGame()
        {
            Instance.Exit();
        }

        protected override void Initialize()
        {
            Thread.CurrentThread.Name = "Monogame Thread";
            Main.GlobalGraphicsDevice = base.GraphicsDevice;

            // Create camera.
            Camera = new Camera();
            Camera.Zoom = 0.5f;

            // Find content directory...
            ContentDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");

            // Create content manager.
            ContentManager = new RazeContentManager(this.GraphicsDevice, ContentDirectory);

            // Create the UI layout object.
            LayoutUI = new LayoutUserInterface(new UserInterface(GraphicsDevice,
                                               new MonoGameMouseProvider(),
                                               new MonoGameKeyboardProvider(Window),
                                               new MonoGameScreenProvider(GraphicsDevice),
                                               new RazeContentProvider(ContentManager)));
            LayoutUI.DrawUI += (ui) =>
            {
                ScreenManager.DrawUI(SpriteBatch, ui);
            };

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
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // Load some default fonts.
            MediumFont = ContentManager.Load<GameFont>("Fonts/MediumFont");
            MediumFont.Size = 24;

            // Load loading icon atlas.
            LoadingIconSprite = new AnimatedSprite(ContentManager.Load<Texture2D>("Textures/LoadingIconAtlas"), 128, 128, 60);
            LoadingIconSprite.Pivot = new Vector2(0.5f, 0.5f);

            // Create the main sprite atlas.
            SpriteAtlas = new SpriteAtlas(2048, 2048);

            // Loading missing texture sprite.
            MissingTextureSprite = SpriteAtlas.Add("Textures/MissingTexture");
            MissingTextureSprite.Pivot = new Vector2(0.5f, 1f); // Bottom center.

            // Temporarily load tiles here.
            TreeTile = SpriteAtlas.Add("Textures/TileComps/Trees");
            MountainTile = SpriteAtlas.Add("Textures/TileComps/Mountain");
            TileShadowTopRight = SpriteAtlas.Add("Textures/TileShadowTopRight");
            TileShadowTopLeft = SpriteAtlas.Add("Textures/TileShadowTopLeft");
            TileShadowBottomRight = SpriteAtlas.Add("Textures/TileShadowBottomRight");
            TileShadowBottomLeft = SpriteAtlas.Add("Textures/TileShadowBottomLeft");
            HouseTile = SpriteAtlas.Add("Textures/TileComps/House");

            // Load definitions.
            string defPath = Path.Combine(ContentDirectory, "Defs");
            DefDatabase = new DefDatabase();
            Debug.StartTimer("Load def files");
            DefDatabase.AddAllFromDirectory(defPath);
            Debug.StopTimer(true);
            Debug.StartTimer("Parse & resolve defs");
            DefDatabase.Load();
            Debug.StopTimer(true);

            // Tile loading from defs.
            Debug.StartTimer("Tile def load");
            DefFactory<Tile, TileDef>.Init("Tile");
            Debug.StopTimer(true);

            Debug.StartTimer("Tile comp def load");
            DefFactory<TileComponent, TileCompDef>.Init("TileComp");
            Debug.StopTimer(true);

            Debug.StartTimer("Entity comp def load");
            DefFactory<Entity, EntityDef>.Init("Entity");
            Debug.StopTimer(true);

            SpriteAtlas.Pack(false);

            Loop.Start();         
        }

        protected override void UnloadContent()
        {
            Debug.Log("Unloading content...");
        }

        protected override void EndRun()
        {
            ContentManager.Dispose();
            ScreenManager.Shutdown();
            Debug.Shutdown();
            thisProcess.Dispose();
            thisProcess = null;
            Instance = null;
            LayoutUI.Dispose();
            base.EndRun();
        }

        protected override void Update(GameTime gameTime)
        {
            ClientBounds = Window.ClientBounds;
            Loop.Update();
        }

        protected override void Draw(GameTime gameTime)
        {
            Loop.Draw(SpriteBatch);
        }

        protected override void EndDraw()
        {
            // Do not present the device to the screen, this is handled in the Loop class.
            Loop.Present();
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
            LayoutUI.Draw();
        }
    }
}