namespace MagmaVRC.UdonVM
{
    /// <summary>Global counters for all <see cref="FastUdonVM"/> instances. Counting is off unless <see cref="Enabled"/> is set.</summary>
    public static class FastUdonVMStats
    {
        public static bool Enabled;
        public static long Instructions;
        public static long Externs;
        public static long FusedHits;
        public static long FusedFallbacks;
        public static long SiteCacheHits;
        public static long SiteCacheMisses;

        public static void Reset()
        {
            Instructions = Externs = FusedHits = FusedFallbacks = SiteCacheHits = SiteCacheMisses = 0;
        }
    }
}
