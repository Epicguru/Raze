using System;
using Microsoft.Xna.Framework;

namespace Raze.Defs
{
    public class CommanderDef : SoldierDef
    {
        public uint Rank;
        public Vector2 Position;
        public Type CommanderClass;

        public override string ToString()
        {
            return base.ToString() + $", Rank: {Rank}, Pos: {Position}, Class Type: {CommanderClass?.FullName ?? "null"}";
        }
    }
}
