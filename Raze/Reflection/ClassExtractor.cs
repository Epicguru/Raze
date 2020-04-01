using GVS.World;
using System;
using System.Reflection;

namespace GVS.Reflection
{
    public class ClassExtractor
    {
        public Assembly TargetAssembly { get; }
        
        public ClassExtractor() : this(Assembly.GetExecutingAssembly())
        {

        }

        public ClassExtractor(Assembly targetAssembly)
        {
            TargetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(TargetAssembly));
        }

        public void ScanAll(Action<Type> foundTile, Action<Type> foundTileComp)
        {
            var a = TargetAssembly;
            if (a == null)
            {
                Debug.Error("Assembly is null, cannot scan.");
                return;
            }

            Debug.StartTimer($"Scanning {a.FullName}");

            var types = a.GetTypes();
            Debug.Trace($"Scanning all {types.Length} types in {a.FullName} for game classes (Tiles, TileComponents, Entities...)");

            Type tileType = typeof(Tile);
            Type tileCompType = typeof(TileComponent);
            foreach (var t in types)
            {
                if (!t.IsClass)
                    continue;

                if (t != tileType && tileType.IsAssignableFrom(t))
                {
                    foundTile?.Invoke(t);
                }
                else if (t != tileCompType && tileCompType.IsAssignableFrom(t))
                {
                    foundTileComp?.Invoke(t);
                }
            }

            Debug.StopTimer(true);
        }
    }
}
