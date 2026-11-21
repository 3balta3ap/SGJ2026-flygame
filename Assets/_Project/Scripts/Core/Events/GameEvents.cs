using System;

namespace FlyJam
{
    /// 기능끼리 서로를 몰라도 되게 하는 이벤트 버스.
    /// 모듈별 이벤트는 GameEvents.<모듈>.cs에 partial로 추가한다. 구독한 쪽은 반드시 OnDisable에서 해제한다.
    public static partial class GameEvents
    {
        public static event Action RoundStarted;
        public static event Action<bool> RoundEnded;                 // true = 클리어
        public static event Action PlayerHit;
        public static event Action<FlyState> PlayerStateChanged;

        public static void RaiseRoundStarted() => RoundStarted?.Invoke();
        public static void RaiseRoundEnded(bool cleared) => RoundEnded?.Invoke(cleared);
        public static void RaisePlayerHit() => PlayerHit?.Invoke();
        public static void RaisePlayerStateChanged(FlyState s) => PlayerStateChanged?.Invoke(s);
    }
}
