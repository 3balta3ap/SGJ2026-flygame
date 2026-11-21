using UnityEngine;

namespace FlyJam
{
    /// Reaction time for the result panel (FLY FACT comparison): seconds from the danger warning appearing
    /// (WarningChanged with a target) to the dodge press. Measured in real time, so slow motion does not shorten it.
    /// Enemies now circle forever, so it measures per warning, not per enemy.
    public class ReactionTracker : MonoBehaviour
    {
        public const string BestKey = "reaction.best";
        public const string AvgKey = "reaction.avg";
        public const string CountKey = "reaction.count";

        float warnedAt = -1f;   // < 0: no warning waiting for a dodge
        float sum;

        public int Count { get; private set; }
        public float Best { get; private set; } = -1f;
        public float Average => Count > 0 ? sum / Count : -1f;

        void OnEnable()
        {
            GameEvents.WarningChanged += OnWarning;
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.WarningChanged -= OnWarning;
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.RoundEnded -= OnRoundEnded;
        }

        void OnWarning(IMovingObstacle target)
        {
            if (target == null) { warnedAt = -1f; return; }
            if (warnedAt < 0f) warnedAt = Time.unscaledTime;   // a new target while still warned keeps the first time
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            if (warnedAt < 0f) return;
            float rt = Time.unscaledTime - warnedAt;
            warnedAt = -1f;   // one measurement per warning
            Count++;
            sum += rt;
            if (Best < 0f || rt < Best) Best = rt;
            GameEvents.RaiseReactionMeasured(rt);
        }

        void OnRoundEnded(bool cleared)
        {
            if (Count <= 0) return;
            SessionResult.SetStat(BestKey, Best);
            SessionResult.SetStat(AvgKey, Average);
            SessionResult.SetStat(CountKey, Count);
        }
    }
}
