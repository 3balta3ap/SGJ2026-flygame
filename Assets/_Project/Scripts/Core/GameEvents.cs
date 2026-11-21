using System;
using UnityEngine;

namespace FlyJam
{
    /// 기능끼리 서로를 몰라도 되게 하는 이벤트 버스. 구독한 쪽은 반드시 OnDisable에서 해제한다.
    public static class GameEvents
    {
        public static event Action RoundStarted;
        public static event Action<bool> RoundEnded;                 // true = 클리어
        public static event Action<Vector3> GoalReached;             // v1 호환 (현재 미사용)
        public static event Action PlayerHit;
        public static event Action<FlyState> PlayerStateChanged;
        public static event Action<Vector3, Vector3> PlayerDodged;   // 위치, 회피 방향
        public static event Action PerfectDodge;
        public static event Action<Vector3> MateAppeared;            // 운동맹 '팟'
        public static event Action MateTouched;
        public static event Action<bool, Vector3> CourtshipFinished; // 성공 여부, 짝 위치
        public static event Action<float> ReactionMeasured;          // 초

        public static void RaiseRoundStarted() => RoundStarted?.Invoke();
        public static void RaiseRoundEnded(bool cleared) => RoundEnded?.Invoke(cleared);
        public static void RaiseGoalReached(Vector3 p) => GoalReached?.Invoke(p);
        public static void RaisePlayerHit() => PlayerHit?.Invoke();
        public static void RaisePlayerStateChanged(FlyState s) => PlayerStateChanged?.Invoke(s);
        public static void RaisePlayerDodged(Vector3 pos, Vector3 dir) => PlayerDodged?.Invoke(pos, dir);
        public static void RaisePerfectDodge() => PerfectDodge?.Invoke();
        public static void RaiseMateAppeared(Vector3 p) => MateAppeared?.Invoke(p);
        public static void RaiseMateTouched() => MateTouched?.Invoke();
        public static void RaiseCourtshipFinished(bool ok, Vector3 p) => CourtshipFinished?.Invoke(ok, p);
        public static void RaiseReactionMeasured(float s) => ReactionMeasured?.Invoke(s);
    }
}
