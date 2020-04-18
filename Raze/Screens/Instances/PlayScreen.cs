using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Raze.Entities;
using Raze.Entities.Instances;
using Raze.Networking;
using Raze.Reflection;
using Raze.World;
using Raze.World.Generation;
using Raze.World.Tiles;
using Raze.World.Tiles.Components;

namespace Raze.Screens.Instances
{
    public partial class PlayScreen : GameScreen
    {
        public bool HostMode { get; set; } = false;

        private int receivedChunks;
        private int expectedChunks;

        public PlayScreen() : base("Play Screen")
        {
        }

        public override void Load()
        {
            Net.OnHumanPlayerConnect += AddPlayerItem;
            Net.OnHumanPlayerDisconnect += RemovePlayerItem;

            LoadUIData();
            LoadGenericMode();

            if (HostMode)
            {
                LoadHostMode();
            }
            else
            {
                LoadRemoteMode();
            }

        }

        /// <summary>
        /// Loads tile defs, entity defs, tileComp defs etc.
        /// Things that need to be loaded just to play the game regardless of client/server mode.
        /// </summary>
        private void LoadGenericMode()
        {
            // Called to load regardless of whether it's host mode or remote mode.

            // Load tiles, tileComps, entities, if they are not already loaded.
            if(Tile.RegisteredCount == 0)
            {
                LoadingScreenText = "Loading object definitions...";
                new ClassExtractor().ScanAll(
                    t =>
                    {
                        // Tiles.
                        Tile.Register(t);
                    },
                    t =>
                    {
                        // Tile components.
                        TileComponent.Register(t);
                    });
            }
        }

        private void LoadHostMode()
        {
            // Create an instance of the isometric map.
            LoadingScreenText = "Creating map...";
            Main.Map = new IsoMap(100, 100, 3);

            // Generate isometric map.
            LoadingScreenText = "Generating map...";
            GenerateMap();

            // Load by creating an internal server and connecting local client.
            LoadingScreenText = "Creating server...";
            Net.Server = new GameServer(7777, 8);
            try
            {
                Net.Server.Start();
            }
            catch(Exception e)
            {
                Debug.Error("Failed to open internal server! Is the port occupied?");
                Debug.Error("Exception: ", e);
                Manager.ChangeScreen<MainMenuScreen>();
                return;
            }

            LoadingScreenText = "Connecting local client...";
            Net.Client = new GameClient();
            bool connected = Net.Client.Connect("localhost", 7777, out string error);
            if (!connected)
            {
                Debug.Error("CRITICAL SHIT YO! Failed to connect to local host! How does that even happen?!?!");
                Debug.Error(error);
                Manager.ChangeScreen<MainMenuScreen>();
            }
        }

        private void LoadRemoteMode()
        {
            // Load by receiving data from remote server and loading that map.
            // Assumes that the client is already connected (from ConnectScreen).
            receivedChunks = 0;
            expectedChunks = 0;

            // Register handler.
            Net.Client.SetHandler(NetMessageType.Data_BasicServerInfo, this.HandleBasicServerInfo);
            Net.Client.SetHandler(NetMessageType.Data_WorldChunk, this.HandleWorldData);

            // Request basic world info.
            LoadingScreenText = "Waiting for server info...";
            NetOutgoingMessage basicReq = Net.Client.CreateMessage(NetMessageType.Req_BasicServerInfo);
            Net.Client.SendMessage(basicReq, NetDeliveryMethod.ReliableUnordered);
            Debug.Trace("Requested basic server info, wait for response...");

            const int SLEEP = 10;
            const int MAX_TIME = 20000;
            const int MAX_ITERATIONS = MAX_TIME / SLEEP;

            for (int i = 0; i < MAX_ITERATIONS; i++)
            {
                // Make sure we receive messages.
                Net.Client.Update();

                // If this is not zero then it means we have already got world basic info.
                if(expectedChunks != 0)
                {
                    float percentage = 100f * ((float)receivedChunks / expectedChunks);
                    LoadingScreenText = $"Downloading world data: {percentage:F0}%";
                }
                
                System.Threading.Thread.Sleep(SLEEP);

                if(receivedChunks >= expectedChunks && expectedChunks != 0)
                {
                    // Neat, all downloaded.
                    Debug.Trace($"Finished downloading {receivedChunks}/{expectedChunks} chunks of data.");
                    Main.Map.SendPlaceMessageToAll();
                    break;
                }
            }

            // Make sure that we got all chunks.
            if(receivedChunks < expectedChunks)
            {
                Debug.Error($"Failed to download all world chunk data in time ({MAX_TIME} ms). Downloaded {receivedChunks} of {expectedChunks}.");
                Manager.ChangeScreen<MainMenuScreen>();
                // URGTODO re-implement me.
                //GeonBit.UI.Utils.MessageBox.ShowMsgBox("Download error", $"Failed to download all data chunks time ({receivedChunks}/{expectedChunks}). Connection is too slow or the server closed.");
            }
        }

        private void HandleBasicServerInfo(byte id, NetIncomingMessage msg)
        {
            // Read data...
            Point3D mapSize = msg.ReadPoint3D();
            expectedChunks = msg.ReadInt32();

            Debug.Trace("Got basic info from server!");
            Debug.Trace($"Server's map is {mapSize}");
            Debug.Trace($"Expecting {expectedChunks} chunks of world data.");

            // Create map instance.
            Main.Map = new IsoMap(mapSize.X, mapSize.Y, mapSize.Z);

            // Now request all of the world data!
            LoadingScreenText = "Requesting map data...";
            NetOutgoingMessage toSend = Net.Client.CreateMessage(NetMessageType.Req_WorldChunks);
            Net.Client.SendMessage(toSend, NetDeliveryMethod.ReliableUnordered);
        }

        private void HandleWorldData(byte id, NetIncomingMessage msg)
        {
            receivedChunks++;
            int length = msg.ReadInt32();
            int startIndex = msg.ReadInt32();

            for (int i = 0; i < length; i++)
            {
                // Read tile ID. 0 means air.
                ushort tileID = msg.ReadUInt16();

                if (tileID == 0) // If air then there is no more data to read.
                    continue;

                int finalIndex = startIndex + i;
                Tile newTile = Tile.CreateInstance(tileID);
                Main.Map.SetTileInternal(finalIndex, newTile);

                // Deserialize tile data. Includes tile components.
                newTile.ReadData(msg, true);
            }
        }

        public override void Unload()
        {
            Net.Server?.Dispose();
            Net.Server = null;

            Net.Client?.Dispose();
            Net.Client = null;

            var map = Main.Map;
            Main.Map = null;
            map.Dispose();
        }

        public override void Update()
        {
            //if (Input.IsKeyJustDown(Keys.F))
            //{
            //    Main.Camera.UpdateViewBounds = !Main.Camera.UpdateViewBounds;
            //    Debug.Log($"Toggled update view bounds: {Main.Camera.UpdateViewBounds}");
            //}

            if (Input.IsKeyJustDown(Keys.R))
            {
                Tile underMouse = Input.TileUnderMouse;
                if (underMouse != null)
                {
                    var spawned = new DevTroop();
                    spawned.Position = underMouse.Position;

                    spawned.Activate();
                }
            }

            // Debug update camera movement. Allows to move using WASD and zoom using E and Q.
            UpdateCameraMove();

            // Update client and server if they are not null.
            // TODO quit game if client disconnects.
            Net.Server?.Update();
            Net.Client?.Update();

            Main.Map.Update();
            Entity.UpdateAll();
        }

        public override void Draw(SpriteBatch sb)
        {
            Main.Map.Draw(sb);
            Entity.DrawAll(sb);
        }

        public override void DrawUI(SpriteBatch sb)
        {
            Entity.DrawAllUI(sb);
        }

        private static void UpdateCameraMove()
        {
            Vector2 input = Vector2.Zero;
            if (Input.IsKeyDown(Keys.A))
                input.X -= 1;
            if (Input.IsKeyDown(Keys.D))
                input.X += 1;
            if (Input.IsKeyDown(Keys.S))
                input.Y += 1;
            if (Input.IsKeyDown(Keys.W))
                input.Y -= 1;
            input.NormalizeSafe();

            const float CHANGE_SPEED = 0.9f;
            const float CHANGE_UP_SPEED = 1f / 0.9f;
            int zoomChange = 0;
            if (Input.IsKeyDown(Keys.E))
                zoomChange += 1;
            if (Input.IsKeyDown(Keys.Q))
                zoomChange -= 1;
            if (Input.IsKeyJustDown(Keys.NumPad0))
                zoomChange = 420;
            if (Input.IsKeyJustDown(Keys.NumPad1))
                zoomChange = 69;
            if (Input.IsKeyJustDown(Keys.NumPad2))
                zoomChange = 69420;

            if (zoomChange != 0)
            {
                switch (zoomChange)
                {
                    case 420:
                        Main.Camera.Zoom = 0.5f;
                        break;
                    case 69:
                        Main.Camera.Zoom *= 2f;
                        break;
                    case 69420:
                        Main.Camera.Zoom *= 0.5f;
                        break;
                    default:
                        Main.Camera.Zoom *= (zoomChange > 0 ? CHANGE_UP_SPEED : CHANGE_SPEED);
                        break;
                }

                Main.Camera.Zoom = MathHelper.Clamp(Main.Camera.Zoom, 0.02f, 10f);
            }

            const float BASE_SPEED = 128f * 5f;
            Main.Camera.Position += input * BASE_SPEED * Time.deltaTime * (1f / Main.Camera.Zoom);
        }

        private void GenerateMap()
        {
            var map = Main.Map;
            Random r = new Random(50);
            Noise n = new Noise(r.Next(0, 10000));

            LoadingScreenText = "Generating map... Placing tiles...";

            // First iteration to place tiles.
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Depth; y++)
                {
                    for (int z = 0; z < map.Height; z++)
                    {
                        const float SCALE = 0.035f;
                        Vector2 offset = new Vector2(500, 300);
                        Color c = (x + y) % 2 == 0 ? Color.White : Color.Lerp(Color.Black, Color.White, 0.95f);
                        float perlin = n.GetPerlin(x * SCALE + offset.X, y * SCALE + offset.Y, z * SCALE);
                        bool place = z == 0 || perlin >= 0.7f;

                        // Prevent floating tiles.
                        var below = map.GetTile(x, y, z - 1);
                        if (z != 0 && (below == null || below is WaterTile))
                            place = false;

                        if (place)
                        {
                            const float WATER_HEIGHT = 0.5f;
                            const float SAND_HEIGHT = 0.55f;

                            Tile t;
                            if (z == 0 && perlin < WATER_HEIGHT)
                                t = new WaterTile();
                            else if (perlin < SAND_HEIGHT)
                                t = new SandTile();
                            else
                                t = new GrassTile();

                            map.SetTileInternal(x, y, z, t);

                            //if (t is WaterTile)
                            //    c = Color.White;

                            t.BaseSpriteTint = c.LightShift(0.85f + 0.15f * ((z + 1f) / map.Height));
                            if (t is WaterTile)
                            {
                                t.BaseSpriteTint = t.BaseSpriteTint.Multiply(Color.DeepSkyBlue);
                                t.BaseSpriteTint = t.BaseSpriteTint.LightShift(0.45f + (perlin / WATER_HEIGHT) * 0.8f);
                            }
                        }
                    }
                }
            }

            LoadingScreenText = "Generating map... Decorating tiles...";

            // Second iteration to place mountains, trees and all that stuff.
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Depth; y++)
                {
                    for (int z = 0; z < map.Height; z++)
                    {
                        Tile t = map.GetTile(x, y, z);
                        if (t == null)
                            continue;
                        if (t is WaterTile || t is SandTile)
                            continue;

                        Tile above = map.GetTile(x, y, z + 1);
                        if (above == null)
                        {
                            if (r.NextDouble() < 0.1f)
                            {
                                t.AddComponent(new Mountain(), 0);
                            }
                            else if (r.NextDouble() < 0.15f)
                            {
                                t.AddComponent(new Trees(), 0);
                            }
                            else if (r.NextDouble() < 0.02f)
                            {
                                t.AddComponent(new House(), 0);
                            }
                        }
                    }
                }
            }

            map.SendPlaceMessageToAll();
        }
    }
}
