using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IObstacle> PlayerHitBy;      // what hit the player. Raised right before PlayerHit
        public static event Action<float, float> HealthChanged; // current HP, max HP
        public static event Action<string> PlayerDied;          // reason: "Hit" | "Starved" (= result screen title key)
        public static event Action<float> HealRequested;        // HP to add (e.g., food)

        public static void RaisePlayerHitBy(IObstacle source) => PlayerHitBy?.Invoke(source);
        public static void RaiseHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied(string reason) => PlayerDied?.Invoke(reason);
        public static void RaiseHealRequested(float amount) => HealRequested?.Invoke(amount);
    }
}
