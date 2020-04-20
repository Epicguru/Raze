using Newtonsoft.Json;
using System;

namespace Raze.Defs
{
    public class GameType
    {
        [NonSerialized]
        public Type Type;

        public GameType()
        {

        }

        public GameType(Type t)
        {
            this.Type = t;
        }

        public static implicit operator Type(GameType t)
        {
            return t.Type;
        }

        public static implicit operator GameType(Type t)
        {
            return new GameType(t);
        }
    }
}
