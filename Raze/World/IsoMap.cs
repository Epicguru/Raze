using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Networking;

namespace Raze.World
{
    public class IsoMap : IDisposable
    {
        public const int TILE_SIZE = 256;
        public const int TARGET_TILES_PER_MESSAGE = 2000;

        /// <summary>
        /// The size of this map in tiles, on the X axis. (Towards screen bottom right)
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The size of this map in tiles, on the Y axis. (Towards screen bottom left)
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// The size of this map in tiles, on the Z axis. (Towards screen top)
        /// </summary>
        public int Height { get; private set; }

        public float SingleTileDepth { get; private set; }

        private readonly Tile[] tiles;
        private readonly int tilesPerHeightLayer;

        public IsoMap(int width, int depth, int height)
        {
            this.Width = width;
            this.Depth = depth;
            this.Height = height;
            this.SingleTileDepth = 1f / (width * height * depth - 1);
            this.tilesPerHeightLayer = width * depth;

            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Must be greater than zero!");
            if (depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth), "Must be greater than zero!");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Must be greater than zero!");

            tiles = new Tile[width * depth * height];

            Debug.Log($"Created new IsoMap, {width}x{depth}x{height} for total {width * depth * height} tiles.");
        }

        public int GetIndex(int x, int y, int z)
        {
            return x + y * Width + z * tilesPerHeightLayer;
        }

        internal Point3D GetCoordsFromIndex(int index)
        {
            int x = index % Width;
            int y = (index / Width) % Depth;
            int z = index / (Width * Depth);

            return new Point3D(x, y, z);
        }

        internal Point GetTileDrawPosition(Point3D mapCoords)
        {
            const int HALF_TILE = TILE_SIZE / 2;
            const int QUARTER_TILE = TILE_SIZE / 4;

            int x = HALF_TILE * mapCoords.Y;
            int y = QUARTER_TILE * mapCoords.Y;

            x -= HALF_TILE * mapCoords.X;
            y += QUARTER_TILE * mapCoords.X;

            y -= HALF_TILE * mapCoords.Z;

            return new Point(x, y);
        }

        public Vector2 GetTileDrawPosition(Vector3 mapCoords)
        {
            const float HALF_TILE = TILE_SIZE / 2f;
            const float QUARTER_TILE = TILE_SIZE / 4f;

            float x = HALF_TILE * mapCoords.Y;
            float y = QUARTER_TILE * mapCoords.Y;

            x -= HALF_TILE * mapCoords.X;
            y += QUARTER_TILE * mapCoords.X;

            y -= HALF_TILE * mapCoords.Z;

            return new Vector2(x, y);
        }

        public float GetTileDrawDepth(Vector3 mapCoords)
        {
            const float MAX = 0.9f;

            // TODO clamp or warn when out of bounds.
            // TODO move calculations such as maxIndex out of here.

            // This is the amount of tiles in a height level.
            float heightStep = Width * Depth;

            // Generate an index for the coordinates.
            // Index increases by 1 for every Width unit, then for every Depth unit, and finally for every Height unit.
            // This means that every Width row will be drawn, then every Depth row will be stacked, then each Height layer will be stacked.
            float index = mapCoords.X + mapCoords.Y * Width + mapCoords.Z * heightStep;

            // Calculate a max index for depth. The max index is the index of the tile that exists at the bottom center edge of the map,
            // 1 height layer above the max height for this map.
            // The reason for the extra height level is so that components can be drawn on top of the top height layer tiles.
            int maxIndex = Width * Depth * (Height + 1) - 1; // Have to add 1 on to the height so that the top tiles can draw components without clipping out.

            // Return the final depth value. The maximum intended depth value is MAX (0.9) to allow for
            // Entities to be drawn on top of the map.
            return MathHelper.Clamp((index / maxIndex) * MAX, 0f, MAX);
        }

        public Vector2 GetGroundPositionFromWorldPosition(Vector2 flatWorldPos, out TileSide side)
        {
            const int H = TILE_SIZE / 2;
            const int Q = TILE_SIZE / 4;

            // Assumes that it is over the z = 0 layer.
            const float Z = 0f;


            (float inX, float inY) = flatWorldPos;
            inX -= H;
            inY -= H;

            // Don't ask, it just works.
            var y = (inY + Z * H) / (2 * Q) + inX / (2 * H);
            var x = y - inX / H;

            // Get the fractional part of the coordinates to compare local x and y.
            int ix = (int)x;
            int iy = (int)y;
            float fx = x - ix;
            float fy = y - iy;

            side = fx < fy ? TileSide.Right : TileSide.Left;

            return new Vector2(x + 1, y + 1);
        }

        public Vector2 GetGroundPositionFromWorldPosition(Vector2 flatWorldPos)
        {
            return GetGroundPositionFromWorldPosition(flatWorldPos, out TileSide _);
        }

        public Color GetMapTint(Tile tile)
        {
            var pos = tile.Position;
            Color color = (pos.X + pos.Y) % 2 == 0 ? Color.White : Color.Lerp(Color.Black, Color.White, 0.95f);
            color = color.LightShift(0.85f + 0.15f * ((tile.Position.Z + 1f) / Height));
            if (tile.IsType("Water"))
            {
                //color = color.LightShift(0.45f + (perlin / WATER_HEIGHT) * 0.8f);
            }

            return color;
        }

        public void Update()
        {
            // URGTODO implement me.
        }

        public bool IsPointInRange(int x, int y, int z)
        {
            return x >= 0 && x < Width && y >= 0 && y < Depth && z >= 0 && z < Height;
        }

        public bool IsPointInRange(Point3D point)
        {
            return IsPointInRange(point.X, point.Y, point.Z);
        }

        public Tile GetTile(int x, int y, int z)
        {
            return IsPointInRange(x, y, z) ? tiles[GetIndex(x, y, z)] : null;
        }

        public Tile GetTile(Point3D point)
        {
            return GetTile(point.X, point.Y, point.Z);
        }

        public bool SetTile(int x, int y, int z, Tile tile)
        {
            // Check that the tile is not already placed.
            if(tile != null)
            {
                if(tile.Map != null)
                {
                    Debug.Error($"Tile {tile} is already placed on the map somewhere! Cannot place again!");
                    return false;
                }
            }

            if(IsPointInRange(x, y, z))
            {
                // Get the current tile at that position and send message if not null and enabled.
                int index = GetIndex(x, y, z);
                Tile current = tiles[index];
                if(current != null)
                {
                    current.UponRemoved(this);
                    current.Map = null;
                    current.Position = Point3D.Zero;
                }

                tiles[index] = tile;
                if(tile != null)
                {
                    // Set position and map reference.
                    tile.Position = new Point3D(x, y, z);
                    tile.Map = this;

                    // Send message, if enabled.
                    tile.UponPlaced(this);
                }

                return true;
            }
            else
            {
                Debug.Error($"Position for tile ({x}, {y}, {z}) is out of bounds!");
                return false;
            }
        }

        internal void SetTileInternal(int x, int y, int z, Tile tile)
        {
            // Set position and map reference.
            tile.Position = new Point3D(x, y, z);
            tile.Map = this;

            tiles[GetIndex(x, y, z)] = tile;
        }

        internal void SetTileInternal(int index, Tile tile)
        {
            // Set position and map reference.
            tile.Position = GetCoordsFromIndex(index);
            tile.Map = this;

            tiles[index] = tile;
        }

        internal void SendPlaceMessageToAll()
        {
            foreach (Tile tile in tiles)
            {
                tile?.UponPlaced(this);
            }
        }

        internal int GetNumberOfNetChunks()
        {
            int chunks = (int)Math.Ceiling((double)tiles.Length / TARGET_TILES_PER_MESSAGE);

            return chunks;
        }

        internal NetOutgoingMessage NetSerializeAllTiles(NetPeerBase peer, int chunkIndex)
        {
            // Determine starting index of this chunk.
            int startIndex = chunkIndex * TARGET_TILES_PER_MESSAGE;

            // Determine actual length of this chunk.
            int length = MathHelper.Min(tiles.Length - startIndex, TARGET_TILES_PER_MESSAGE);

            // Create new message.
            var msg = peer.CreateMessage(NetMessageType.Data_WorldChunk);
            msg.Write(length);
            msg.Write(startIndex);

            // Write all tile data.
            for (int j = 0; j < length; j++)
            {
                var tile = tiles[startIndex + j];
                if (tile == null)
                {
                    msg.Write((ushort)0);
                }
                else
                {
                    msg.Write(tile.ID);

                    tile.WriteData(msg, true);
                }
            }

            return msg;
        }

        public void Draw(SpriteBatch spr)
        {
            var bounds = GetDrawBounds();

            // Have to draw from bottom layer up, from top to bottom.
            for (int z = 0; z < Height; z++)
            {
                for (int x = bounds.sx; x <= bounds.ex; x++)
                {
                    for (int y = bounds.sy; y <= bounds.ey; y++)
                    {
                        Tile tile = tiles[GetIndex(x, y, z)]; // Could use the GetIndex() method, but I don't know if it would get inlined.
                        if (tile == null)
                            continue;

                        tile.Draw(spr);
                    }
                }
            }
        }

        public (int sx, int sy, int ex, int ey) GetDrawBounds()
        {
            var cam = Main.Camera;
            var bounds = cam.WorldViewBounds;
            Vector2 topLeft = GetGroundPositionFromWorldPosition(bounds.Location.ToVector2());
            Vector2 topRight = GetGroundPositionFromWorldPosition(bounds.Location.ToVector2() + new Vector2(bounds.Width, 0f));
            Vector2 bottomLeft = GetGroundPositionFromWorldPosition(bounds.Location.ToVector2() + bounds.Size.ToVector2() - new Vector2(bounds.Width, 0f));
            Vector2 bottomRight = GetGroundPositionFromWorldPosition(bounds.Location.ToVector2() + bounds.Size.ToVector2());

            //Debug.Box(bounds, Color.Red.AlphaShift(0.05f));
            //Point size = new Point(10, 10);
            //Debug.Point(bounds.Location.ToVector2(), 10f, Color.DarkOliveGreen.AlphaShift(0.8f));
            //Debug.Point(bounds.Location.ToVector2() + new Vector2(bounds.Width, 0f), 10f, Color.DarkOliveGreen.AlphaShift(0.8f));
            //Debug.Point(bounds.Location.ToVector2() + bounds.Size.ToVector2() - new Vector2(bounds.Width, 0f), 10f, Color.DarkOliveGreen.AlphaShift(0.8f));
            //Debug.Point(bounds.Location.ToVector2() + bounds.Size.ToVector2(), 10f, Color.DarkOliveGreen.AlphaShift(0.8f));


            int startX = Min(topLeft.X, topRight.X, bottomLeft.X, bottomRight.X);
            int startY = Min(topLeft.Y, topRight.Y, bottomLeft.Y, bottomRight.Y);

            int endX = Max(topLeft.X, topRight.X, bottomLeft.X, bottomRight.X);
            int endY = Max(topLeft.Y, topRight.Y, bottomLeft.Y, bottomRight.Y);

            // Offset bounds downwards to make them not clip out the height.
            endX += Height;
            endY += Height;

            Point3D start = ClampToWorld(new Point3D(startX, startY, 0));
            Point3D end = ClampToWorld(new Point3D(endX, endY, 0));

            return (start.X, start.Y, end.X, end.Y);
        }

        public Point3D ClampToWorld(Point3D tileCoordinates)
        {
            Point3D newPos = new Point3D();

            newPos.X = MathHelper.Clamp(tileCoordinates.X, 0, Width - 1);
            newPos.Y = MathHelper.Clamp(tileCoordinates.Y, 0, Depth - 1);
            newPos.Z = MathHelper.Clamp(tileCoordinates.Z, 0, Height - 1);

            return newPos;
        }

        private int Min(params float[] args)
        {
            if (args.Length == 0)
                return -1;

            int min = int.MaxValue;
            foreach (var value in args)
            {
                if (value < min)
                    min = (int)Math.Floor(value);
            }

            return min;
        }

        private int Max(params float[] args)
        {
            if (args.Length == 0)
                return -1;

            int max = int.MinValue;
            foreach (var value in args)
            {
                if (value > max)
                    max = (int)Math.Ceiling(value);
            }

            return max;
        }

        public void Dispose()
        {
            
        }

        public override string ToString()
        {
            return $"IsoMap [{Width}x{Depth}x{Height}]";
        }

        public enum TileSide
        {
            Left,
            Right
        }
    }
}
