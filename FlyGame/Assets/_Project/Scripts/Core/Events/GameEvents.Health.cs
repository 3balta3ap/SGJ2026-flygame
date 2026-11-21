using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IObstacle> PlayerHitBy;              // what touched the player. Raised right before PlayerHit
        public static event Action<float, float> HealthChanged;         // current HP, max HP
        public static event Action<string> PlayerDied;                  // reason: "Hit" | "Starved" (= result title key)
        public static event Action<float> HealRequested;                // fraction of max HP to add (apple = 0.2)
        public static event Action<float, string> DamageRequested;      // fraction of max HP to remove, reason (mash battle lost)
        public static event Action<float, bool> MaxHpRequested;         // new max HP (Level feature), grow = also add the increase to current HP (false at round start)

        public static void RaisePlayerHitBy(IObstacle source) => PlayerHitBy?.Invoke(source);
        public static void RaiseHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied(string reason) => PlayerDied?.Invoke(reason);
        public static void RaiseHealRequested(float ratio) => HealRequested?.Invoke(ratio);
        public static void RaiseDamageRequested(float ratio, string reason) => DamageRequested?.Invoke(ratio, reason);
        public static void RaiseMaxHpRequested(float max, bool grow) => MaxHpRequested?.Invoke(max, grow);
    }
}
