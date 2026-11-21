using UnityEngine;

namespace FlyJam
{
    /// On touching the mate: pause session -> freeze player & mate -> run courtship QTE -> win on success, mate flees on failure.
    /// Mate references auto-resolve from MateAI.Current if left empty.
    public class CourtshipFlow : MonoBehaviour
    {
        [SerializeField] CourtshipQTE qte;
        [SerializeField] GameSession session;                 // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;                 // Auto-filled from GameContext if null
        [SerializeField] PlanetFollowCamera followCamera;     // Auto-filled from GameContext if null
        [SerializeField] MateAI mateAI;                       // Auto-filled from MateAI.Current if null

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

            // Courtship behavior: the female may flee to test the suitor
            if (mateVisibility != null) mateVisibility.ForceVisible = false;
            mateAI.Freeze(false);
            mateAI.Flee();
            if (mateGoal != null) mateGoal.Rearm();
            player.SetInputEnabled(true);
        }
    }
}
