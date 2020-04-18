using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Sprites;
using Raze.World;

namespace Raze.Entities
{
    public abstract class Entity
    {
        #region Static

        public static int SpawnedCount
        {
            get { return activeEntities.Count; }
        }

        private static readonly List<Entity> activeEntities = new List<Entity>();
        private static readonly List<Entity> pendingEntities = new List<Entity>();

        private static void Register(Entity e)
        {
            if (e == null)
            {
                Debug.Error("Cannot register null entity!");
                return;
            }
            if (e.internalState != 0)
            {
                Debug.Error($"Entity {e} is not in the correct state (expected 0 got {e.internalState}), cannot register again.");
                return;
            }
            if (e.IsDestroyed)
            {
                Debug.Error("Entity is already destroyed, it cannot be registered now!");
                return;
            }

            // Update state and add to the pending list. The state variable allow the skipping of checking the array.
            e.internalState = 1;
            pendingEntities.Add(e);
        }

        internal static void UpdateAll()
        {
            // Add pending entities.
            foreach (var entity in pendingEntities)
            {
                if(entity != null && !entity.IsDestroyed && entity.internalState == 1)
                {
                    entity.internalState = 2;
                    entity.Map = Main.Map;
                    activeEntities.Add(entity);

                    // Give it the spawn message.
                    entity.UponActivated();
                }
                else if(entity != null)
                {
                    entity.IsDestroyed = true;
                    entity.internalState = 3;
                    entity.Map = null;
                    // Don't give it the destroyed message because it was never spawned.
                }
            }
            pendingEntities.Clear();

            for (int i = 0; i < activeEntities.Count; i++)
            {
                var entity = activeEntities[i];

                if (entity.IsDestroyed)
                {
                    // Give despawn message, then set state.
                    entity.UponDestroyed();
                    entity.internalState = 3;
                    entity.Map = null;

                    activeEntities.RemoveAt(i);
                    i--;
                    continue;
                }

                // TODO catch exceptions and handle.
                entity.Update();
            }
        }

        internal static void DrawAll(SpriteBatch spr)
        {
            foreach (var entity in activeEntities)
            {
                if (entity.IsDestroyed)
                    continue;

                // TODO catch exceptions and handle.
                entity.Draw(spr);
            }
        }

        internal static void DrawAllUI(SpriteBatch spr)
        {
            foreach (var entity in activeEntities)
            {
                if (entity.IsDestroyed)
                    continue;

                // TODO catch exceptions and handle.
                entity.DrawUI(spr);
            }
        }

        #endregion

        public string Name { get; protected set; } = "No-name";
        public IsoMap Map { get; internal set; }
        public Vector3 Position;
        public bool IsDestroyed { get; private set; } = false;

        /// <summary>
        /// The internal state of the entity, related to how it is registered, updated and removed from the world.
        /// <para>0: None. Entity has been instantiated but nothing else. </para>
        /// <para>1: Entity has been registered and is now pending entry to world. </para>
        /// <para>2: Entity has now moved out of the pending list and is now being updated. </para>
        /// <para>3: Entity has been removed from the world. Note that the entity can still be 'destroyed' even if the state is not 3: check the <see cref="IsDestroyed"/> flag. </para>
        /// </summary>
        private byte internalState = 0;

        ~Entity()
        {
            // If the entity is garbage collected and still has state 0, it is probably an oversight from the developer
            // who failed to call Activate().
            if(internalState == 0)
            {
                Debug.Warn($"Entity {this} was garbage collected in state 0. Did you forget to call Activate()?");
            }
        }

        /// <summary>
        /// Causes this entity to be spawned into the world. If your entity isn't showing up or rendering,
        /// make sure this has been called.
        ///<para></para>
        /// Repeated calls, or calls when entity is in invalid state (such as destroyed)
        /// will have no effect and will not log an error.
        /// </summary>
        public void Activate()
        {
            if (IsDestroyed)
                return;

            if (internalState != 0)
                return;

            Register(this);
        }
        
        /// <summary>
        /// Called once when the entity has been spawned into the world, after a call to <see cref="Activate"/>.
        /// From here you can do any logic necessary for when the entity should be spawned. This will be called before
        /// the first call to <see cref="Update"/>.
        /// </summary>
        protected virtual void UponActivated()
        {

        }

        /// <summary>
        /// The default behaviour causes this entity to be immediately removed from the world.
        /// Custom implementations by each entity type may have different behaviour.
        /// To know if the entity has been immediately destroyed (as it will be with default implementation),
        /// check the <see cref="IsDestroyed"/> flag after calling this method.
        /// This is called the first frame AFTER the entity has been marked as destroyed.
        /// </summary>
        public virtual void Destroy()
        {
            IsDestroyed = true;
        }

        /// <summary>
        /// Called once when the entity has been removed from the world. This will not be called if the entity was
        /// never actually spawned into the world first (see <see cref="UponActivated"/>).
        /// A call to <see cref="Destroy"/> will normally, but not necessarily (children can give custom implementations),
        /// result in a call to this method soon afterwards.
        /// This is called immediately before the entity is actually removed, so any changes to world state or other
        /// entities are still valid in here.
        /// </summary>
        protected virtual void UponDestroyed()
        {

        }

        /// <summary>
        /// Called once per frame to update the entity's logic.
        /// </summary>
        protected virtual void Update()
        {
            
        }

        /// <summary>
        /// Called once per frame to draw the entity into the world. Avoid 'logic' code in here, such as Input
        /// or movement.
        /// </summary>
        /// <param name="sb">The SpriteBatch to draw the entity with. Positions and sizes will be in world-space.</param>
        protected virtual void Draw(SpriteBatch sb)
        {

        }

        /// <summary>
        /// Called once per frame to draw UI. This can be used to draw any kind of UI, but in-world UI is preferred
        /// since the draw order of this is basically random.
        /// </summary>
        /// <param name="sb">The SpriteBatch to draw with. Positions and sizes will be in screen-space.</param>
        protected virtual void DrawUI(SpriteBatch sb)
        {

        }

        protected void DrawSprite(SpriteBatch sb, Sprite sprite)
        {
            this.DrawSprite(sb, sprite, Color.White, Vector2.Zero);
        }

        protected virtual void DrawSprite(SpriteBatch sb, Sprite sprite, Color tint, Vector2 offset, float rotation = 0f, float scale = 1f, SpriteEffects effects = SpriteEffects.None)
        {
            sb.Draw(sprite, GetDrawPosition() + offset, tint, GetDrawDepth(), rotation, scale, effects);
        }

        public virtual Vector2 GetDrawPosition()
        {
            if (Map == null)
                return Vector2.Zero;

            Vector2 pos = Map.GetTileDrawPosition(this.Position);

            // Add on the offset to place at center of tile surface that corresponds to current position.
            pos.X += IsoMap.TILE_SIZE * 0.5f;
            pos.Y += IsoMap.TILE_SIZE * 0.25f;

            return pos;
        }

        public virtual float GetDrawDepth()
        {
            // The depth of the tile above - to ensure that it draws on top of the target tile.
            float tileAboveDepth = Map.GetTileDrawDepth(this.Position + new Vector3(0, 0, 1f));

            // The additional depth to draw on top of components of that tile.
            float toAdd = 0.5f * Map.SingleTileDepth;

            return tileAboveDepth + toAdd;
        }

        public override string ToString()
        {
            return $"{(Name ?? "null-name")} {Position}";
        }
    }
}
