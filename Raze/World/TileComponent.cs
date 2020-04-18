using System;
using System.Collections.Generic;
using System.Reflection;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Raze.World
{
    public class TileComponent
    {
        #region Static stuff

        public static int RegisteredCount
        {
            get
            {
                return all.Count;
            }
        }

        private static readonly List<ConstructorInfo> tileCompCreators = new List<ConstructorInfo>();
        private static readonly Dictionary<Type, ushort> type2ID = new Dictionary<Type, ushort>();
        private static readonly List<Type> all = new List<Type>();
        private static readonly object[] noArgs = new object[0];

        public static ushort Register<T>() where T : TileComponent
        {
            return Register(typeof(T));
        }

        public static ushort Register(Type t)
        {
            if (t == null)
            {
                Debug.Error("Cannot register null tile component type.");
                return 0;
            }
            string name = t.FullName;
            if (!typeof(TileComponent).IsAssignableFrom(t))
            {
                Debug.Error($"Type {name} does not inherit from TileComponent, so you can't register it as a tile component!");
                return 0;
            }
            if (type2ID.ContainsKey(t))
            {
                Debug.Error($"{name} is already registered!");
                return 0;
            }

            // Look for zero arg public constructor.
            var creator = t.GetConstructor(new Type[] { });
            if (creator == null)
            {
                Debug.Error($"Tile component {name} does not have a zero-argument public constructor. It needs one to be able to load and save correctly.");
                return 0;
            }

            ushort id = (ushort)(type2ID.Count + 1);

            type2ID.Add(t, id);
            tileCompCreators.Add(creator);
            all.Add(t);

            return id;
        }

        public static Type GetTileComponentType(ushort id)
        {
            if (id == 0)
                return null;

            int index = id - 1;
            if (all.Count == 0 || index >= all.Count)
            {
                Debug.Warn($"Failed to get type of tile component for ID {id}: ID out of bounds. Have all tiles been registered yet?");
                return null;
            }

            return all[index];
        }

        public static TileComponent CreateInstance(ushort id)
        {
            if (id == 0)
                return null;

            int index = id - 1;
            if (all.Count == 0 || index >= all.Count)
            {
                Debug.Warn($"Failed to create tile component instance for ID {id}: ID out of bounds. Have all tiles been registered yet?");
                return null;
            }

            TileComponent instance = tileCompCreators[index].Invoke(noArgs) as TileComponent;

            return instance;
        }

        public static ushort GetID(Type t)
        {
            if (t == null)
                return 0;

            return type2ID.ContainsKey(t) ? type2ID[t] : (ushort)0;
        }

        #endregion

        public Tile Tile { get; internal set; }
        public int Index { get; internal set; } = -1;
        public IsoMap Map
        {
            get
            {
                return Tile.Map;
            }
        }
        public Point3D Position
        {
            get
            {
                return Tile.Position;
            }
        }
        public ushort ID { get; internal set; }

        public TileComponent()
        {
            ID = GetID(this.GetType());
        }

        public virtual float GetDrawDepth()
        {
            // Get the draw depth of the tile above. This is where the base (index = 0) component would draw.
            float aboveDepth = Map.GetTileDrawDepth(Position + new Point3D(0, 0, 1));

            // Get a 'nudge' based on our index. Higher indexes draw at higher levels.
            // The nudge value is limited to half of a tile depth, allowing for entities to draw on top of components.
            float indexNudge = Map.SingleTileDepth * 0.5f * ((float) (Index + 1) / (Tile.MaxComponentCount + 1));

            return MathHelper.Clamp(aboveDepth + indexNudge, 0f, 1f);
        }

        public virtual Vector2 GetDrawPosition()
        {
            return Map.GetTileDrawPosition(Position + new Vector3(0, 0, Tile.Height));
        }

        protected internal virtual void UponAdded(Tile addedTo)
        {

        }

        protected internal virtual void UponRemoved(Tile removedFrom)
        {

        }

        /// <summary>
        /// Called when this tile component needs to be serialized across the network.
        /// Should write any data that has changed since the last write, or all data is the
        /// forSpawn parameter is true.
        /// </summary>
        /// <param name="msg">The message to write to.</param>
        /// <param name="forSpawn">If true, write all data (that changes during gameplay). If false, only write the data that may have changed since last write.</param>
        public virtual void WriteData(NetBuffer msg, bool forSpawn)
        {
            
        }

        /// <summary>
        /// Called when this tile component needs to be deserialized across the network.
        /// Should read any data that has changed since the last read, or all data is the
        /// forSpawn parameter is true.
        /// This read data should be applied immediately.
        /// </summary>
        /// <param name="msg">The message to write to.</param>
        /// <param name="forSpawn">If true, read all data (that changes during gameplay). If false, only read the data that may have changed since last read.</param>
        public virtual void ReadData(NetBuffer msg, bool forSpawn)
        {

        }

        public virtual void Update()
        {

        }

        public virtual void Draw(SpriteBatch spr)
        {

        }
    }
}
