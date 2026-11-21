using System;

namespace FlyJam
{
    /// Event bus that lets features communicate without knowing each other.
    /// Add module-specific events as partial files (GameEvents.<Module>.cs). Subscribers must always unsubscribe in OnDisable.
    public static partial class GameEvents
    {
        public static event Action RoundStarted;
        public static event Action<bool> RoundEnded;                 // true = cleared
        public static event Action PlayerHit;
        public static event Action<FlyState> PlayerStateChanged;

        public static void RaiseRoundStarted() => RoundStarted?.Invoke();
        public static void RaiseRoundEnded(bool cleared) => RoundEnded?.Invoke(cleared);
        public static void RaisePlayerHit() => PlayerHit?.Invoke();
        public static void RaisePlayerStateChanged(FlyState s) => PlayerStateChanged?.Invoke(s);
    }
}
