using UnityEngine;

namespace FlyJam
{
    /// Raises MateTouched when the player reaches the female.
    /// After a courtship ends (Rearm), the player must first move rearmDistance away: no automatic restart (E03, E04).
    public class MateGoal : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float reachDistance = 0.9f;
        [SerializeField] float rearmDistance = 2.5f;

        bool armed = true;
        bool waitForSeparation;
        GameSession session;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null && ctx.Player != null) player = ctx.Player.transform;
                session = ctx.Session;
            }
        }

        void Update()
        {
            if (player == null) return;
            if (session != null && (session.Ended || session.IsPaused)) return;
            if (PlayerProgress.Fever) return;   // flying during the level-up fever: courtship starts after landing

            float d = Vector3.Distance(player.position, transform.position);
            if (waitForSeparation)
            {
                if (d > rearmDistance)
                {
                    waitForSeparation = false;
                    armed = true;
                }
                return;
            }
            if (armed && d <= reachDistance)
            {
                armed = false;
                GameEvents.RaiseMateTouched();
            }
        }

        public void Rearm()
        {
            armed = false;
            waitForSeparation = true;
        }
    }
}
