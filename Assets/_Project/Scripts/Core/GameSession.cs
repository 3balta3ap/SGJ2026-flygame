using System.Collections;
using UnityEngine;

namespace FlyJam
{
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;
        [SerializeField] SphereWalker mate;
        [SerializeField] MateAI mateAI;
        [SerializeField] MateGoal mateGoal;
        [SerializeField] MotionBlindVisibility mateVisibility;
        [SerializeField] CourtshipQTE courtship;
        [SerializeField] PlanetFollowCamera followCamera;
        [SerializeField] ReactionTracker reactions;
        [SerializeField] NearMissDirector nearMiss;
        [SerializeField] float timeLimit = 45f;
        [SerializeField] float resultDelay = 2.0f;

        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, timeLimit - Elapsed);
        public float Normalized => Mathf.Clamp01(Elapsed / timeLimit);
        public bool Ended { get; private set; }
        public bool Courting { get; private set; }

        void OnEnable()
        {
            GameEvents.MateTouched += OnMateTouched;
            GameEvents.PlayerHit += OnPlayerHit;
        }

        void OnDisable()
        {
            GameEvents.MateTouched -= OnMateTouched;
            GameEvents.PlayerHit -= OnPlayerHit;
        }

        void Start()
        {
            Time.timeScale = 1f;
            GameEvents.RaiseRoundStarted();
        }

        void Update()
        {
            if (Ended || Courting) return;   // 구애 중에는 시간(=하늘)도 멈춘다
            Elapsed += Time.deltaTime;
            if (Elapsed >= timeLimit) End(false, "TimeUp");
        }

        void OnMateTouched()
        {
            if (Ended || Courting) return;
            Courting = true;
            player.SetInputEnabled(false);
            mateAI.Freeze(true);
            mateVisibility.ForceVisible = true;
            if (followCamera != null) followCamera.SuppressLookOut = true;
            player.FaceTowards(mate.transform.position);
            mate.FaceTowards(player.transform.position);
            courtship.Begin(OnCourtshipDone);
        }

        void OnCourtshipDone(bool success)
        {
            GameEvents.RaiseCourtshipFinished(success, mate.transform.position);
            if (followCamera != null) followCamera.SuppressLookOut = false;
            Courting = false;

            if (success)
            {
                player.SetState(FlyState.Celebrating);
                mate.SetState(FlyState.Celebrating);
                End(true, "Love");
                return;
            }

            // 구애 7단계: 암컷은 쫓아오나 시험하려고 도망가기도 한다
            mateVisibility.ForceVisible = false;
            mateAI.Freeze(false);
            mateAI.Flee();
            mateGoal.Rearm();
            player.SetInputEnabled(true);
        }

        void OnPlayerHit()
        {
            if (Ended || Courting) return;
            End(false, "Hit");
        }

        void End(bool cleared, string reason)
        {
            Ended = true;
            player.SetInputEnabled(false);
            if (!cleared) player.SetState(FlyState.Disabled);

            SessionResult.Set(cleared, Elapsed, reason);
            SessionResult.SetStats(
                reactions != null ? reactions.Best : -1f,
                reactions != null ? reactions.Average : -1f,
                reactions != null ? reactions.Count : 0,
                nearMiss != null ? nearMiss.PerfectCount : 0);

            GameEvents.RaiseRoundEnded(cleared);
            StartCoroutine(GoToResultAfterDelay());
        }

        IEnumerator GoToResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDelay);
            SceneFlow.GoToResult();
        }
    }
}
