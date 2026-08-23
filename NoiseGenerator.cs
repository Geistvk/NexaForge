namespace NexaForge
{
    // Einfacher, deterministischer Hash-basierter "Noise"-Generator.
    // Kein echter Perlin-/Simplex-Noise, aber reicht für unregelmäßige,
    // reproduzierbare Flecken/Muster auf einem Raster - z.B. Erzvorkommen.
    public static class NoiseGenerator
    {
        // Pseudozufälliger, aber für (x, z, seed) immer gleicher Wert zwischen 0 und 1
        public static float Hash(int x, int z, int seed)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + x * 668265263;
                h = h * 374761393 + z * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        // Geglätteter Wert durch Mittelung benachbarter Zellen (billiges "Smooth Noise",
        // erzeugt zusammenhängende Flecken statt komplett zufälligem Rauschen)
        public static float SmoothValue(int x, int z, int seed)
        {
            float total = Hash(x, z, seed) * 4f;
            total += Hash(x - 1, z, seed);
            total += Hash(x + 1, z, seed);
            total += Hash(x, z - 1, seed);
            total += Hash(x, z + 1, seed);
            return total / 8f;
        }
    }
}
