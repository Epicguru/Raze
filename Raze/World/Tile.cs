using System;
using System.Collections.Generic;
using System.Reflection;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Defs;
using Raze.Sprites;

namespace Raze.World
{
    public abstract class Tile : Defined<TileDef>
    {
        #region Static stuff

        private static string[] idToDefName = new string[ushort.MaxValue + 1];
        private static Dictionary<Type, Func<TileDef, Tile>> constructors = new Dictionary<Type, Func<TileDef, Tile>>();

        public static void PostLoadDefs()
        {
            Def rootDef = Main.DefDatabase.Get("Tile");
            if (rootDef == null)
                throw new Exception("Failed to find tile root def.");

            var tileDefs = Main.DefDatabase.GetChildren(rootDef);

            // TODO sort defs alphabetically to ensure that they are always in the same order and therefore have same ID.
            ushort globalID = 0;

            Debug.Trace($"There are {tileDefs.Count} tile defs loaded.");

            constructors.Clear();
            foreach (var d in tileDefs)
            {
                var def = d as TileDef;
                Type definedType = def.TileClass;

                if(definedType == null)
                {
                    Debug.Error($"Tile def {d.Name} is missing a tile class. Could it not be loaded?");
                    continue;
                }

                var defType = def.GetType();

                // Assign ID.
                ushort id = ++globalID;
                idToDefName[id] = def.Name;
                def.TileID = id;

                if (constructors.ContainsKey(defType))
                {
                    continue;
                }

                MethodInfo[] methods = defType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo customMethod = null;

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute(typeof(DefinedProviderAttribute)) == null)
                        continue;

                    if (!definedType.IsAssignableFrom(method.ReturnType))
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' returns a {method.ReturnType.Name} which is not assignable to {definedType.Name}.");
                        continue;
                    }

                    if (method.ContainsGenericParameters)
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has a generic parameter which is not allowed.");
                        continue;
                    }

                    if(method.GetParameters().Length != 1)
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has {method.GetParameters().Length} parameters, but there should be exactly 1 of a type assignable from TileDef.");
                        continue;
                    }

                    if (!method.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(TileDef)))
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has a parameter of type {method.GetParameters()[0].ParameterType.Name} which is not assignable from TileDef and is therefore invalid.");
                        continue;
                    }

                    customMethod = method;
                    break;
                }

                if (customMethod != null)
                {
                    constructors.Add(defType, (inDef) => (Tile)customMethod.Invoke(null, new object[]{inDef}));
                }
                else
                {
                    var allInClass = def.TileClass.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    ConstructorInfo found = null;
                    foreach (var con in allInClass)
                    {
                        var param = con.GetParameters();
                        if (param.Length != 1)
                            continue;

                        if (param[0].ParameterType.IsAssignableFrom(typeof(TileDef)))
                        {
                            found = con;
                            break;
                        }
                    }

                    if (found == null)
                    {

                        Debug.Error($"Tile definition {def.Name} has {def.TileClass.Name} as defined class, but the defined class has no public or private one-argument constructor and the {def.Class.Name} class has no DefinedProvider method.");
                    }
                    else
                    {
                        constructors.Add(defType, (inDef) => (Tile)found.Invoke(new object[]{inDef}));
                    }
                }
            }

            Debug.Trace($"Created or found {constructors.Count} of {tileDefs.Count} constructors.");
        }

        public static Tile Create(ushort id)
        {
            return Create(idToDefName[id]);
        }

        public static Tile Create(string defName)
        {
            if (defName == null)
            {
                Debug.Warn("Null def name passed into Create!");
                return null;
            }

            if (Main.DefDatabase.Get(defName) is TileDef def)
                return Create(def);

            Debug.Warn($"Failed to find tile def for name {defName}");
            return null;
        }

        public static Tile Create(TileDef def)
        {
            if (def == null)
            {
                Debug.Warn("TileDef passed into Create is null. Perhaps the def is not loaded?");
                return null;
            }

            if (constructors.TryGetValue(def.GetType(), out var f))
            {
                var created = f.Invoke(def);
                created?.ApplyDef(def);
                return created;
            }
            return null;
        }

        #endregion

        public static Color ShadowColor = Color.Black.AlphaShift(0.5f);

        public ushort ID
        {
            get
            {
                return Def.TileID;
            }
        }
        public string Name
        {
            get
            {
                return Def.Name;
            }
        }

        /// <summary>
        /// The sprite that is rendered as the 'base': as the ground. If null, nothing is drawn apart from
        ///  the components.
        /// </summary>
        public Sprite BaseSprite
        {
            get;
            set;
        }

        /// <summary>
        /// The 'permanent' tint that the ground is drawn with. This ONLY affects the ground sprite, not the components.
        /// </summary>
        public Color BaseSpriteTint { get; set; } = Color.White;
        /// <summary>
        /// The 'temporary' tint that the ground is drawn with. When this tile is drawn,
        /// this value is multiplied by the <see cref="BaseSpriteTint"/> tint and then the tile is drawn with the resulting color.
        /// Once the tile is drawn, this tint is reset to white. This allows for easy manipulation of ground color.
        /// </summary>
        public Color TemporarySpriteTint { get; set; } = Color.White;
        /// <summary>
        /// The relative height of this tile, where 1 is a full height tile, 0.5 is a half-height tile and 0
        /// would be just the bottom face of the tile. Used to draw components and entities at the correct height.
        /// </summary>
        public float Height { get; protected set; } = 1f;

        public Point3D Position { get; internal set; }
        public IsoMap Map { get; internal set; }
        public int MaxComponentCount
        {
            get
            {
                return components.Length;
            }
        }
        public Vector2 DrawPosition { get; protected set; }

        private readonly TileComponent[] components = new TileComponent[8];

        protected Tile(TileDef def)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
        }

        public override void ApplyDef(TileDef def)
        {
            BaseSprite = def.Sprite;
            BaseSpriteTint = def.BaseTint;
        }

        public float GetDrawDepth()
        {
            return Map.GetTileDrawDepth(Position);
        }

        protected internal virtual void UponPlaced(IsoMap map)
        {
            DrawPosition = map.GetTileDrawPosition(Position).ToVector2();
        }

        protected internal virtual void UponRemoved(IsoMap map)
        {
            // Should this also call the relevant UponRemoved method on all the components?
        }

        public virtual void Update()
        {
            // Update all components.
            foreach (var comp in components)
            {
                // TODO catch or log exceptions.
                comp?.Update();
            }
        }

        public virtual void Draw(SpriteBatch spr)
        {
            // Draw sprite into world.
            if(BaseSprite != null)
            {
                Color c = BaseSpriteTint.Multiply(TemporarySpriteTint);
                c = c.Multiply(Map.GetMapTint(this));

                spr.Draw(BaseSprite, DrawPosition, c, GetDrawDepth());
                TemporarySpriteTint = Color.White;

                // Draw tile shadows, where necessary.
                if(Position.Z != 0 && Map.GetTile(Position.X, Position.Y, Position.Z + 1) == null)
                {
                    var toDraw = GetShouldDrawShadows();
                    DrawShadows(spr, toDraw.topRight, toDraw.topLeft, toDraw.bottomRight, toDraw.bottomLeft);
                }
            }

            // Draw all components.
            foreach (var comp in components)
            {
                // TODO catch or log exceptions.
                comp?.Draw(spr);
            }
        }

        protected virtual (bool topRight, bool topLeft, bool bottomRight, bool bottomLeft) GetShouldDrawShadows()
        {
            Tile toRight = Map.GetTile(Position.X - 1, Position.Y, Position.Z);
            Tile toBLeft = Map.GetTile(Position.X + 1, Position.Y, Position.Z);
            Tile toLeft = Map.GetTile(Position.X, Position.Y - 1, Position.Z);
            Tile toBRight = Map.GetTile(Position.X, Position.Y + 1, Position.Z);

            return (toRight == null, toLeft == null, toBRight == null, toBLeft == null);
        }

        protected virtual void DrawShadows(SpriteBatch spr, bool topRight, bool topLeft, bool bottomRight, bool bottomLeft)
        {
            const float MULTI = 1f / 20f;
            float depthNudge = Map.SingleTileDepth * 0.5f * MULTI; // This nudge places it above the tile and below the first component.

            if (topRight)
            {
                spr.Draw(Main.TileShadowTopRight, DrawPosition, ShadowColor, GetDrawDepth() + depthNudge);
            }
            if (topLeft)
            {
                spr.Draw(Main.TileShadowTopLeft, DrawPosition, ShadowColor, GetDrawDepth() + depthNudge);
            }
            if (bottomLeft)
            {
                spr.Draw(Main.TileShadowBottomLeft, DrawPosition, ShadowColor, GetDrawDepth() + depthNudge);
            }
            if (bottomRight)
            {
                spr.Draw(Main.TileShadowBottomRight, DrawPosition, ShadowColor, GetDrawDepth() + depthNudge);
            }
        }

        public bool CanAddComponent(TileComponent tc, int index)
        {
            return CanAddComponent(tc, index, out int _);
        }

        public bool CanAddComponent(TileComponent tc, int index, out int errorCode)
        {
            if (index < 0 || index >= components.Length)
            {
                // Index out of bounds!
                errorCode = 0;
                return false;
            }
            if (tc == null)
            {
                // Null component!
                errorCode = 1;
                return false;
            }
            if (tc.Tile != null)
            {
                // Component already has parent!
                errorCode = 2;
                return false;
            }
            if (components[index] != null)
            {
                // Component slot is not empty! (slot is already occupied)
                errorCode = 3;
                return false;
            }

            errorCode = -1;
            return true;
        }

        public bool AddComponent(TileComponent tc, int index, bool sendMessage = true)
        {
            bool canAdd = CanAddComponent(tc, index);
            if (!canAdd)
            {
                Debug.Error($"Cannot add component {tc} to tile {this} at index {index}!");
                CanAddComponent(tc, index, out int code);
                Debug.Error($"Error code: {code}");
                
                return false;
            }

            // Tell the component that we are it's parent (also 'assigns' position).
            tc.Tile = this;
            tc.Index = index;

            // Write it to the array.
            components[index] = tc;

            // Tell the component that it was just added.
            if (sendMessage)
            {
                tc.UponAdded(this);
            }

            return true;
        }

        public bool RemoveComponent(int index, bool sendMessage = true)
        {
            if (index < 0 || index >= components.Length)
            {
                Debug.Error($"Index {index} is out of bounds for removal of a component. Min: 0, Max: {components.Length - 1} inclusive.");
                return false;
            }

            // Tell the component that is is about to be removed, if it is not null.
            var current = components[index];
            if(current != null)
            {
                current.Index = -1;
                if (sendMessage)
                    current.UponRemoved(this);
            }

            // Clear from the array.
            components[index] = null;

            return true;
        }

        /// <summary>
        /// Called when the tile needs to be send over the network to another player.
        /// Will only be called on the server.
        /// You should only write data that actually needs to be synchronized. For example, it is not
        /// necessary to send the name of the tile if the name never changes or has no gameplay importance.
        /// Default implementation writes the <see cref="BaseSpriteTint"/>, and all components.
        /// </summary>
        /// <param name="msg">The message to write data to.</param>
        /// <param name="forSpawn">If <see langword="true"/>, then all required data should be written, since the receiving client knows nothing about this tile. If <see langword="false"/>, then only data that might have changed since spawned needs to be sent.</param>
        public virtual void WriteData(NetBuffer msg, bool forSpawn)
        {
            // Tint color.
            msg.Write(this.BaseSpriteTint);

            // Count how many components there are.
            byte compCount = 0;
            foreach (var comp in components)
            {
                if (comp != null)
                    compCount++;
            }
            msg.Write(compCount);

            // Write component data...
            for (int i = 0; i < components.Length; i++)
            {
                byte index = (byte)i;
                var comp = components[index];
                if(comp != null)
                {
                    // Write the index of the component, because some gaps may be left in the array.
                    msg.Write(index);

                    // If this is the first time that it is sent, also send the ID so that it can be spawned.
                    if (forSpawn)
                    {
                        msg.Write(comp.ID);
                    }

                    comp.WriteData(msg, forSpawn);
                }
            }
        }

        /// <summary>
        /// Called when the tile has been send from the server to this client.
        /// Here the data needs to be read and immediately applied.
        /// Default implementation reads the <see cref="BaseSpriteTint"/>, and all components.
        /// </summary>
        /// <param name="msg">The message to read data from.</param>
        /// <param name="forSpawn">If <see langword="true"/>, then all required data should be read, since this client knows nothing about this tile. If <see langword="false"/>, then only data that might have changed since spawned needs to be read.</param>
        public virtual void ReadData(NetBuffer msg, bool forSpawn)
        {
            this.BaseSpriteTint = msg.ReadColor();

            byte compCount = msg.ReadByte();
            for (int i = 0; i < compCount; i++)
            {
                byte index = msg.ReadByte();

                if (forSpawn)
                {
                    ushort id = msg.ReadUInt16();
                    var newComp = TileComponent.CreateInstance(id);
                    RemoveComponent(index, false);
                    AddComponent(newComp, index, false);
                }

                var current = components[index];
                current.ReadData(msg, forSpawn);
            }
        }

        public override string ToString()
        {
            return $"{Name} {Position}";
        }
    }
}
