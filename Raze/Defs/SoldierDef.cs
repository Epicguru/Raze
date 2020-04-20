using System.Collections.Generic;

namespace Raze.Defs
{
    public class SoldierDef : Def
    {
        public int Attack;
        public int Defense;
        public float Range = 20f;
        public List<string> Enemies;

        public override string ToString()
        {
            return base.ToString() + $", Attack: {Attack}, Defense: {Defense}, Range: {Range}, Targets: [{string.Join(", ", Enemies)}]";
        }
    }
}
