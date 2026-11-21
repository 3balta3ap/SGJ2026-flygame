using System.Collections.Generic;

namespace FlyJam
{
    /// Round result passed from Game to Result: 3 core fields plus per-feature key-value stats.
    /// Each feature defines its own stat keys as constants (e.g., "reaction.best"); ResultScreen displays them only when present.
    public static class SessionResult
    {
        public static bool Cleared;
        public static float ElapsedSeconds;
        public static string Reason = "";

        static readonly Dictionary<string, float> stats = new Dictionary<string, float>();

        public static void Begin(bool cleared, float elapsed, string reason)
        {
            Cleared = cleared;
            ElapsedSeconds = elapsed;
            Reason = reason;
            stats.Clear();
        }

        public static void SetStat(string key, float value) => stats[key] = value;
        public static bool TryGetStat(string key, out float value) => stats.TryGetValue(key, out value);
    }
}
