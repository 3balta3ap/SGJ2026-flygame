using System.Collections.Generic;

namespace FlyJam
{
    /// Game → Result로 넘기는 결과. 기본 3개 값 + 기능별 통계(키-값).
    /// 통계 키는 각 기능이 상수로 정한다 (예: "reaction.best"). 결과 화면은 키가 있을 때만 보여준다.
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
