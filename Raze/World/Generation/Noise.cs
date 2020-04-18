using System;
using SharpNoise.Modules;

namespace Raze.World.Generation
{
    public class Noise : IDisposable
    {
        /// <summary>
        /// The seed that is used to generate noise.
        /// </summary>
        public int Seed { get; }
        /// <summary>
        /// Toggles the remapper on or off. On by default. The default implementation of the remapper
        /// turns the 'raw' noise from the range -1 to 1 into the range 0 to 1. You can add your own
        /// remapping function by setting the value of <see cref="Remapper"/>.
        /// </summary>
        public bool Remap { get; set; } = true;
        /// <summary>
        /// The remapping function to use when <see cref="Remap"/> is set to true. Default implementation
        /// remaps from -1 to 1 into 0 to 1. If set to null, remapping is not used.
        /// </summary>
        public RemapFunction Remapper { get; set; }

        public delegate float RemapFunction(float input);

        private Perlin Perlin
        {
            get
            {
                if (perlin == null)
                {
                    perlin = new Perlin() { Seed = this.Seed };
                    Debug.Log($"Initialized perlin noise with Freq: {perlin.Frequency}, Octave Count: {perlin.OctaveCount} using persistance: {perlin.Persistence}.");
                }

                return perlin;
            }
            set
            {
                perlin = value;
            }
        }
        private Perlin perlin;

        public Noise(int seed)
        {
            Seed = seed;

            // Default remapper turns the range -1 to 1 into 0 to 1.
            Remapper = (float input) => 0.5f + input * 0.5f;
        }

        public void SetPerlinPersistence(float persistence)
        {
            Perlin.Persistence = persistence;
        }

        public float GetPerlin(float x, float y)
        {
            float value =  (float)Perlin.GetValue(x, y, 0);
            if (Remap && Remapper != null)
                value = Remapper(value);

            return value;
        }

        public float GetPerlin(float x, float y, float z)
        {
            float value = (float)Perlin.GetValue(x, y, z);
            if (Remap && Remapper != null)
                value = Remapper(value);

            return value;
        }

        public void Dispose()
        {
            Perlin = null;
        }
    }
}
