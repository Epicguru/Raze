using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Defs;
using Raze.Sprites;

namespace Raze.World.Tiles.Components
{
    public abstract class TileComponent : Defined<TileCompDef>
    {
        #region Static stuff
        public static TileComponent Create(TileCompDef def)
        {
            return DefFactory<TileComponent, TileCompDef>.Create(def);
        }

        public static TileComponent Create(string defName)
        {
            return DefFactory<TileComponent, TileCompDef>.Create(defName);
        }

        public static TileComponent Create(ushort defID)
        {
            return DefFactory<TileComponent, TileCompDef>.Create(defID);
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
        public Sprite Sprite { get; set; }
        public Color SpriteTint { get; set; } = Color.White;

        public ushort ID
        {
            get
            {
                return Def.DefID;
            }
        }

        protected TileComponent(TileCompDef def)
        {
            this.Def = def;
        }

        public override void ApplyDef(TileCompDef def)
        {
            // Nothing to do... Yet.
            Sprite = def.Sprite;
            SpriteTint = def.SpriteTint;
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

        public abstract void Update();

        public abstract void Draw(SpriteBatch spr);
    }
}
