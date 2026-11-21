namespace FlyJam
{
    public static class SessionResult
    {
        public static bool Cleared;
        public static float ElapsedSeconds;
        public static string Reason = "";
        public static float BestReaction = -1f;
        public static float AverageReaction = -1f;
        public static int ReactionCount;
        public static int PerfectCount;

        public static void Set(bool cleared, float elapsed, string reason)
        {
            Cleared = cleared;
            ElapsedSeconds = elapsed;
            Reason = reason;
            BestReaction = -1f;
            AverageReaction = -1f;
            ReactionCount = 0;
            PerfectCount = 0;
        }

        public static void SetStats(float best, float average, int count, int perfect)
        {
            BestReaction = best;
            AverageReaction = average;
            ReactionCount = count;
            PerfectCount = perfect;
        }
    }
}
