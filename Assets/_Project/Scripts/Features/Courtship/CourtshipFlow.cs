using UnityEngine;

namespace FlyJam
{
    /// 짝에게 닿으면: 세션 일시정지 → 입력·짝 멈춤 → 구애 QTE → 성공이면 승리, 실패면 짝이 도망.
    /// 짝 관련 참조는 비워 두면 MateAI.Current에서 찾는다.
    public class CourtshipFlow : MonoBehaviour
    {
        [SerializeField] CourtshipQTE qte;
        [SerializeField] GameSession session;                 // 비우면 GameContext에서
        [SerializeField] SphereWalker player;                 // 비우면 GameContext에서
        [SerializeField] PlanetFollowCamera followCamera;     // 비우면 GameContext에서
        [SerializeField] MateAI mateAI;                       // 비우면 MateAI.Current

        SphereWalker mate;
        MateGoal mateGoal;
        MotionBlindVisibility mateVisibility;
        bool running;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (session == null) session = ctx.Session;
                if (player == null) player = ctx.Player;
                if (followCamera == null) followCamera = ctx.FollowCamera;
            }
        }

        void OnEnable() => GameEvents.MateTouched += OnMateTouched;
        void OnDisable() => GameEvents.MateTouched -= OnMateTouched;

        bool ResolveMate()
        {
            if (mateAI == null) mateAI = MateAI.Current;
            if (mateAI == null) return false;
            if (mate == null) mate = mateAI.GetComponent<SphereWalker>();
            if (mateGoal == null) mateGoal = mateAI.GetComponent<MateGoal>();
            if (mateVisibility == null) mateVisibility = mateAI.GetComponent<MotionBlindVisibility>();
            return mate != null;
        }

        void OnMateTouched()
        {
            if (running || qte == null || session == null || player == null) return;
            if (session.Ended || session.IsPaused || !ResolveMate()) return;

            running = true;
            session.Pause(this);
            player.SetInputEnabled(false);
            mateAI.Freeze(true);
            if (mateVisibility != null) mateVisibility.ForceVisible = true;
            if (followCamera != null) followCamera.SuppressLookOut = true;
            player.FaceTowards(mate.transform.position);
            mate.FaceTowards(player.transform.position);
            qte.Begin(OnDone);
        }

        void OnDone(bool success)
        {
            running = false;
            GameEvents.RaiseCourtshipFinished(success, mate.transform.position);
            if (followCamera != null) followCamera.SuppressLookOut = false;
            session.Resume(this);

            if (success)
            {
                player.SetState(FlyState.Celebrating);
                mate.SetState(FlyState.Celebrating);
                session.End(true, "Love");
                return;
            }

            // 구애 7단계: 암컷은 쫓아오나 시험하려고 도망가기도 한다
            if (mateVisibility != null) mateVisibility.ForceVisible = false;
            mateAI.Freeze(false);
            mateAI.Flee();
            if (mateGoal != null) mateGoal.Rearm();
            player.SetInputEnabled(true);
        }
    }
}
