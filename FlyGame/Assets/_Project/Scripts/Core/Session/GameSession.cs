using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Core round lifecycle only: timer, pause, end state, and transition to the Result scene.
    /// Win conditions (e.g., courtship) are driven by features calling End(true, reason).
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;          // Auto-filled from GameContext if null
        [SerializeField] float timeLimit = 45f;
        [SerializeField] float resultDelay = 2.0f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, timeLimit - Elapsed);
        public float Normalized => Mathf.Clamp01(Elapsed / timeLimit);
        public bool Ended { get; private set; }
        /// True while any system holds a pause (e.g., courtship sequence). Freezes timer, sky progression, and hazard spawning.
        public bool IsPaused => pausers.Count > 0;
        public SphereWalker Player => player;

        void Awake()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerHit += OnPlayerHit;
        void OnDisable() => GameEvents.PlayerHit -= OnPlayerHit;

        void Start()
        {
            Time.timeScale = 1f;
            GameEvents.RaiseRoundStarted();
        }

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
            if (Elapsed >= timeLimit) End(false, "TimeUp");
        }

        public void Pause(object by)
        {
            if (by != null) pausers.Add(by);
        }

        public void Resume(object by)
        {
            if (by != null) pausers.Remove(by);
        }

        void OnPlayerHit()
        {
            if (Ended || IsPaused) return;
            End(false, "Hit");
        }

        /// Ends the round. 'reason' selects the result screen title (e.g., "Love", "Hit", "TimeUp").
        public void End(bool cleared, string reason)
        {
            if (Ended) return;
            Ended = true;
            pausers.Clear();
            if (player != null)
            {
                player.SetInputEnabled(false);
                if (!cleared) player.SetState(FlyState.Disabled);
            }

            SessionResult.Begin(cleared, Elapsed, reason);
            GameEvents.RaiseRoundEnded(cleared);   // Each feature records its own stats to SessionResult here
            StartCoroutine(GoToResultAfterDelay());
        }

        IEnumerator GoToResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDelay);
            SceneFlow.GoToResult();
        }
    }
}
